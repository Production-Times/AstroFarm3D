/*
 * ============================================================================
 * INVENTORY ITEM PHYSICS & COLLISION STATE SYSTEM
 * ============================================================================
 * 
 * OVERVIEW
 * --------
 * Your inventory system now has FULL CONTROL over physics and collision
 * for every item state throughout its entire lifecycle.
 * 
 * Each state can have unique settings for:
 * - Rigidbody (gravity, kinematic, mass, drag, constraints)
 * - Colliders (enabled/disabled, trigger mode)
 * - Layers (switch to different collision layers)
 * 
 * ============================================================================
 * 
 * AVAILABLE STATES (15 Total)
 * ----------------------------
 * 
 * 1. FREE STATE
 *    - Item on ground, not interacting
 *    - Physics: Gravity ON, Kinematic OFF, Colliders ON
 * 
 * 2. PLAYER BAG ATTACHED STATE
 *    - Item stored in player inventory
 *    - Physics: Gravity OFF, Kinematic ON, Colliders OFF
 * 
 * 3. PLAYER CARRIED STATE
 *    - Player actively holding item
 *    - Physics: Gravity OFF, Kinematic ON, Colliders OFF
 * 
 * 4. VACUUM CAPTURED STATE
 *    - Item being pulled by vacuum
 *    - Physics: Gravity OFF, Kinematic ON, Colliders OFF
 * 
 * 5. TRACTOR UNLOADED STATE
 *    - Just unloaded from vehicle
 *    - Physics: Gravity ON, Kinematic OFF, Colliders ON
 * 
 * 6. DROP POINT PLACED STATE
 *    - Placed at collection point
 *    - Physics: Gravity OFF, Kinematic ON, Colliders TRIGGER
 * 
 * 7. CONVEYOR BELT PROCESSING STATE
 *    - On conveyor belt
 *    - Physics: Gravity OFF, Kinematic ON, Colliders OFF
 * 
 * 8. PROCESSING MACHINE LOADING STATE
 *    - Loading into machine
 *    - Physics: Gravity OFF, Kinematic ON, Colliders OFF
 * 
 * 9. PROCESSING MACHINE PROCESSING STATE
 *    - Being processed/transformed
 *    - Physics: Gravity OFF, Kinematic ON, Colliders OFF
 * 
 * 10. PROCESSING MACHINE UNLOADING STATE
 *     - Ejecting from machine
 *     - Physics: Gravity OFF, Kinematic ON, Colliders ON
 * 
 * 11. DELIVERY POINT STATE
 *     - At delivery/sale location
 *     - Physics: Gravity OFF, Kinematic ON, Colliders TRIGGER
 * 
 * 12. SELLING STATE
 *     - Being sold/destroyed
 *     - Physics: Gravity OFF, Kinematic ON, Colliders OFF
 * 
 * 13-15. CUSTOM1, CUSTOM2, CUSTOM3 STATES
 *     - Your custom mechanics
 *     - Physics: Fully configurable
 * 
 * ============================================================================
 * 
 * HOW TO USE
 * ----------
 * 
 * METHOD 1: Call convenience methods
 * -----------------------------------
 * item.OnPickedUp();                      // PlayerCarried state
 * item.OnDropped();                       // Free state
 * item.OnPlaced();                        // DropPointPlaced state
 * item.OnAttachedToPlayerBag();           // PlayerBagAttached state
 * item.OnVacuumCaptured();                // VacuumCaptured state
 * item.OnTractorUnloaded();               // TractorUnloaded state
 * item.OnConveyorBeltProcessing();        // ConveyorBeltProcessing state
 * item.OnProcessingMachineLoading();      // ProcessingMachineLoading state
 * item.OnProcessingMachineProcessing();   // ProcessingMachineProcessing state
 * item.OnProcessingMachineUnloading();    // ProcessingMachineUnloading state
 * item.OnDeliveryPoint();                 // DeliveryPoint state
 * item.OnSelling();                       // Selling state
 * 
 * METHOD 2: Set state directly
 * ----------------------------
 * item.SetState(ItemState.ConveyorBeltProcessing);
 * 
 * METHOD 3: Get current state
 * ---------------------------
 * ItemState current = item.GetCurrentState();
 * 
 * ============================================================================
 * 
 * CUSTOMIZING PHYSICS PER STATE
 * ------------------------------
 * 
 * 1. Select any item PREFAB in your project
 * 2. Find the InventoryItem component
 * 3. Expand the state you want to customize (e.g., "Vacuum Captured State")
 * 4. Adjust the physics settings:
 * 
 *    RIGIDBODY SETTINGS:
 *    - Use Gravity: Enable/disable gravity
 *    - Is Kinematic: Enable/disable physics simulation
 *    - Constraints: Lock position/rotation on specific axes
 *    - Collision Detection: Discrete/Continuous
 *    - Mass: Object weight
 *    - Drag: Linear damping
 *    - Angular Drag: Rotational damping
 * 
 *    COLLISION SETTINGS:
 *    - Colliders Enabled: Turn all colliders on/off
 *    - Is Trigger: Make colliders triggers
 * 
 *    LAYER SETTINGS:
 *    - Change Layer: Switch to different layer
 *    - Target Layer: Which layer to use
 * 
 * ============================================================================
 * 
 * EXAMPLE SCRIPTS PROVIDED
 * -------------------------
 * 
 * VacuumCollector.cs
 * - Vacuum items with physics state control
 * - Demonstrates VacuumCaptured state
 * 
 * ProcessingMachine.cs
 * - Full processing pipeline with 3 states
 * - Loading -> Processing -> Unloading
 * 
 * VehicleCargo.cs
 * - Load/unload items from vehicles
 * - Uses TractorUnloaded state
 * 
 * ConveyorBelt.cs
 * - Transport items on conveyor
 * - Uses ConveyorBeltProcessing state
 * 
 * DeliveryPoint.cs
 * - Deliver and sell items
 * - Uses DeliveryPoint and Selling states
 * 
 * DropPoint.cs (Updated)
 * - Now supports sending to conveyor belt
 * - Uses DropPointPlaced state
 * 
 * ItemStateDebugger.cs
 * - Debug tool to visualize states
 * - Logs physics changes
 * 
 * FarmProductionChain.cs
 * - Example integration of all systems
 * - Shows complete production workflow
 * 
 * ============================================================================
 * 
 * STATE CHANGE EVENTS
 * -------------------
 * 
 * Subscribe to state changes:
 * 
 * void Start()
 * {
 *     item.onStateChanged.AddListener(OnItemStateChanged);
 * }
 * 
 * void OnItemStateChanged(ItemState newState)
 * {
 *     Debug.Log($"Item changed to: {newState}");
 *     
 *     if (newState == ItemState.Selling)
 *     {
 *         PlaySellAnimation();
 *     }
 * }
 * 
 * ============================================================================
 * 
 * PRODUCTION CHAIN EXAMPLE
 * ------------------------
 * 
 * 1. HARVEST PHASE
 *    - VacuumCollector captures crops
 *    - Items in VacuumCaptured state (no physics)
 *    - Collected into storage
 * 
 * 2. STORAGE PHASE
 *    - DropPoint receives items
 *    - Items in DropPointPlaced state (trigger colliders)
 *    - Stacked in place
 * 
 * 3. TRANSPORT PHASE
 *    - ConveyorBelt moves items
 *    - Items in ConveyorBeltProcessing state (no collisions)
 *    - Smooth movement controlled by belt
 * 
 * 4. PROCESSING PHASE
 *    - ProcessingMachine transforms items
 *    - Loading -> Processing -> Unloading states
 *    - Full physics control at each step
 * 
 * 5. DELIVERY PHASE
 *    - DeliveryPoint accepts items
 *    - Items in DeliveryPoint state
 *    - Then Selling state before destruction
 * 
 * ============================================================================
 * 
 * BEST PRACTICES
 * --------------
 * 
 * 1. Always set appropriate state when taking control of an item
 * 2. Return to Free state when releasing item to world
 * 3. Customize physics per state in prefab inspector
 * 4. Use state events for visual effects and logic
 * 5. Test different states to ensure correct physics behavior
 * 6. Use ItemStateDebugger during development to visualize states
 * 
 * ============================================================================
 * 
 * TROUBLESHOOTING
 * ---------------
 * 
 * Q: Items falling through floor?
 * A: Check Free state has gravity=true, kinematic=false
 * 
 * Q: Items not moving on conveyor?
 * A: Ensure ConveyorBeltProcessing has kinematic=true
 * 
 * Q: Items colliding when shouldn't?
 * A: Disable colliders or use triggers for that state
 * 
 * Q: Need different physics for specific item?
 * A: Override state settings on that item's prefab
 * 
 * Q: State not changing?
 * A: Check if you're calling the right method
 * 
 * Q: Want to see current state?
 * A: Add ItemStateDebugger component for visual feedback
 * 
 * ============================================================================
 * 
 * CUSTOMIZATION TIPS
 * ------------------
 * 
 * ADD NEW CUSTOM STATE:
 * 1. Already have Custom1, Custom2, Custom3 available
 * 2. Configure physics in prefab inspector
 * 3. Call: item.SetState(ItemState.Custom1);
 * 
 * MODIFY EXISTING STATE:
 * 1. Select item prefab
 * 2. Expand the state section
 * 3. Adjust physics parameters
 * 4. All instances will use new settings
 * 
 * OVERRIDE FOR ONE ITEM:
 * 1. Select specific item prefab (not the base)
 * 2. Modify state settings
 * 3. Only this item type affected
 * 
 * ============================================================================
 */
