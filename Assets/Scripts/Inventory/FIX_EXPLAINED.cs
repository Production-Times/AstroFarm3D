/*
 * ============================================================================
 * THE PIZZA ATTRACTION BUG - FIXED! 🍕
 * ============================================================================
 * 
 * THE PROBLEM:
 * ------------
 * Items were being picked up by the player while "flying" from the DropPoint
 * to the ConveyorBelt. This created a race condition:
 * 
 * OLD FLOW (BUGGY):
 * -----------------
 * 
 * 1. Item placed at DropPoint
 *    State: DropPointPlaced ✓
 *    isPlaced: TRUE ✓
 *    Player can pickup: NO ✓
 * 
 * 2. DropPoint sends to conveyor
 *    - Remove from storedItems
 *    - Call targetConveyorBelt.AddItemToBelt(item)
 *    
 * 3. DURING AddItemToBelt() execution:
 *    State: STILL DropPointPlaced
 *    Parent: Being changed...
 *    Position: Being changed...
 *    ⚠️ Player vacuum sees it and picks it up! ❌
 * 
 * 4. ConveyorBelt sets state
 *    State: ConveyorBeltProcessing
 *    (Too late! Player already took it!)
 * 
 * ============================================================================
 * 
 * THE FIX:
 * --------
 * Set the item state to ConveyorBeltProcessing BEFORE sending it!
 * 
 * NEW FLOW (FIXED):
 * -----------------
 * 
 * 1. Item placed at DropPoint
 *    State: DropPointPlaced ✓
 *    isPlaced: TRUE ✓
 *    Player can pickup: NO ✓
 * 
 * 2. DropPoint prepares to send to conveyor
 *    - item.OnConveyorBeltProcessing() FIRST! ✓
 *    State: ConveyorBeltProcessing ✓
 *    isPlaced: TRUE ✓
 *    Player can pickup: NO ✓
 * 
 * 3. Now safe to send
 *    - Remove from storedItems
 *    - Call targetConveyorBelt.AddItemToBelt(item)
 *    - Item is already in correct state! ✓
 * 
 * 4. ConveyorBelt receives item
 *    State: Already ConveyorBeltProcessing ✓
 *    Player can pickup: NO ✓
 * 
 * ============================================================================
 * 
 * CODE CHANGES:
 * -------------
 * 
 * File: DropPoint.cs
 * Method: SendItemToConveyor()
 * 
 * BEFORE:
 * ```csharp
 * protected virtual void SendItemToConveyor(InventoryItem item)
 * {
 *     if (item == null || targetConveyorBelt == null)
 *         return;
 *     
 *     storedItems.Remove(item);                    // ❌ Still pickable!
 *     
 *     targetConveyorBelt.AddItemToBelt(item);      // ❌ Race condition here!
 *     
 *     RefreshStackPositions();
 * }
 * ```
 * 
 * AFTER:
 * ```csharp
 * protected virtual void SendItemToConveyor(InventoryItem item)
 * {
 *     if (item == null || targetConveyorBelt == null)
 *         return;
 *     
 *     item.OnConveyorBeltProcessing();             // ✓ Set state FIRST!
 *     
 *     storedItems.Remove(item);                    // ✓ Now safe!
 *     
 *     targetConveyorBelt.AddItemToBelt(item);      // ✓ No race condition!
 *     
 *     RefreshStackPositions();
 * }
 * ```
 * 
 * ============================================================================
 * 
 * File: InventoryItem.cs
 * Method: UpdateLegacyFlags()
 * 
 * ALSO ADDED VacuumCaptured to isPlaced states:
 * 
 * ```csharp
 * isPlaced = (currentState == ItemState.DropPointPlaced || 
 *            currentState == ItemState.DeliveryPoint ||
 *            currentState == ItemState.ConveyorBeltProcessing ||
 *            currentState == ItemState.ProcessingMachineLoading ||
 *            currentState == ItemState.ProcessingMachineProcessing ||
 *            currentState == ItemState.ProcessingMachineUnloading ||
 *            currentState == ItemState.VacuumCaptured ||        // ✓ NEW!
 *            currentState == ItemState.Selling);
 * ```
 * 
 * This ensures items being vacuumed by one system can't be stolen by player.
 * 
 * ============================================================================
 * 
 * WHY THIS WORKS:
 * ---------------
 * 
 * PlayerBackpack.CanPickupItem() checks:
 * 
 * if (item.isPlaced)
 *     return false; // ✓ Blocks pickup!
 * 
 * Since we now set the state to ConveyorBeltProcessing BEFORE sending,
 * the item is NEVER in a vulnerable state where the player can pick it up.
 * 
 * ============================================================================
 * 
 * TIMELINE COMPARISON:
 * --------------------
 * 
 * OLD (BUG):
 * Frame 1: Item at DropPoint, State=DropPointPlaced, isPlaced=TRUE
 * Frame 2: Remove from list, State=DropPointPlaced, isPlaced=TRUE
 * Frame 3: AddItemToBelt starts, State=DropPointPlaced ⚠️ VULNERABLE!
 * Frame 4: Player vacuum picks it up ❌ BUG!
 * Frame 5: State changes to ConveyorBeltProcessing (too late)
 * 
 * NEW (FIXED):
 * Frame 1: Item at DropPoint, State=DropPointPlaced, isPlaced=TRUE
 * Frame 2: OnConveyorBeltProcessing(), State=ConveyorBeltProcessing ✓
 * Frame 3: Remove from list, State=ConveyorBeltProcessing, isPlaced=TRUE ✓
 * Frame 4: AddItemToBelt starts, State=ConveyorBeltProcessing ✓ PROTECTED!
 * Frame 5: Player vacuum tries, isPlaced=TRUE, BLOCKED! ✓ NO BUG!
 * 
 * ============================================================================
 * 
 * TESTING:
 * --------
 * 
 * 1. Place 10 items at drop point
 * 2. Enable conveyor sending
 * 3. Stand next to the path from drop point to conveyor
 * 4. Watch items fly past
 * 5. Try to vacuum them up
 * 
 * RESULT: Should see debug log:
 * "[PlayerBackpack] Cannot pickup Pizza: isPlaced = true, State = ConveyorBeltProcessing"
 * 
 * ✓ Items are now PROTECTED during transit!
 * ✓ No more pizza stealing! 🍕
 * 
 * ============================================================================
 */
