/*
 * ========================================
 *  SAW PURCHASE SYSTEM - QUICK GUIDE
 * ========================================
 * 
 * OVERVIEW:
 * Purchase up to 6 saws with cash, each saw count has custom transform layout
 * 
 * ========================================
 * SETUP STEPS
 * ========================================
 * 
 * 1. CREATE SAW CONFIGURATION
 *    - Right-click → Create → AstroFarm → Saw Configuration
 *    - Assign saw prefab
 *    - Set costs for each saw count (1-6)
 * 
 * 2. BAKE TRANSFORM LAYOUTS
 *    - Select SawConfiguration asset in Inspector
 *    - Assign "Reference Vehicle" field to your vehicle GameObject in scene
 *    - For each saw count (1-6):
 *      a. Add that many saws to your vehicle in scene
 *      b. Position them exactly where you want
 *      c. Click "📸 Capture Transforms (X saws)" button
 *      d. Transforms are now saved!
 * 
 * 3. ADD SAW MANAGER
 *    - Add SawManager component to vehicle
 *    - Assign SawConfiguration
 *    - Set Saw Container transform (where saws spawn)
 *    - Saws auto-spawn on Start based on PlayerPrefs
 * 
 * 4. ADD PURCHASE BUTTON (OPTIONAL)
 *    - Add SawPurchaseButton to UI button
 *    - Assign SawManager reference
 *    - Button auto-updates cost, state, etc.
 * 
 * ========================================
 * FEATURES
 * ========================================
 * 
 * ✓ Editor buttons to capture/bake transforms
 * ✓ Apply transforms to scene for testing
 * ✓ Purchase with cash using CashManager
 * ✓ Persistent saw count (PlayerPrefs)
 * ✓ Auto-spawn on game start
 * ✓ Ready-to-use UI button component
 * ✓ Custom inspector for easy management
 * 
 * ========================================
 * EXAMPLE LAYOUTS
 * ========================================
 * 
 * 1 Saw:  [Front Center]
 *         Cost: $100
 * 
 * 2 Saws: [Left] [Right]
 *         Cost: $250
 * 
 * 3 Saws: [Left] [Center] [Right]
 *         Cost: $500
 * 
 * 4 Saws: [FL] [FR]
 *         [BL] [BR]
 *         Cost: $1000
 * 
 * 5 Saws: [L] [C] [R]
 *         [BL] [BR]
 *         Cost: $2000
 * 
 * 6 Saws: [FL] [FC] [FR]
 *         [BL] [BC] [BR]
 *         Cost: $4000
 * 
 * ========================================
 * CODE EXAMPLES
 * ========================================
 * 
 * // Purchase next saw
 * sawManager.TryPurchaseNextSaw();
 * 
 * // Check cost
 * int cost = sawManager.GetCostForNextSaw();
 * 
 * // Check if can afford
 * bool canBuy = sawManager.CanPurchaseNextSaw();
 * 
 * // Get current count
 * int count = sawManager.GetCurrentSawCount();
 * 
 * // Check if maxed
 * bool maxed = sawManager.IsMaxSaws();
 * 
 * // Get all active saws
 * List<HarvesterTool> saws = sawManager.GetActiveSaws();
 * 
 * ========================================
 * PERSISTENCE
 * ========================================
 * 
 * PlayerPrefs Key: "PlayerSawCount"
 * Saves: Whenever saw count changes
 * Loads: On SawManager.Awake()
 * 
 * ========================================
 */

// This file exists only for documentation
// Delete this file if you don't need the guide
