// This is Tiny's base scripts.
// To display a component on a screen, the CustomInfo of
// both the screen and the component should be tagged with
// "[TinyDrillSystem]" and "[TinyDrillSystem-group-####]"
// where "####" is an integer representing the 'group number.'
// Screens will display the info for all components with
// matching 'group numbers.'
//
// Currently supported component types:
// - rotors and advanced rotors
// - pistons
// - drills
//
//TODO: FEATURE - Implement custom rounding with a minimum decimal point count
//
//TODO: CONSIDER - FEATURE - Consider adding a "default screen" mechanism.
//TODO: CONSIDER - FEATURE - Consider implementing custom rounding that
//         doesn't round to a maximum or minimum value until it reaches
//         equality (i.e. for a 0-10 field, 9.99 would round to 9, and 0.0001 would round to 1)
//
//
//
//TODO: FEATURE: BlockRenamerByShipName - Add option to include other grids.  See: https://forum.keenswh.com/threads/code-to-get-localgrid-and-remote-grids-added-blocks.7392342/
//
//TODO: FEATURE: BlockRenamerByShipName - Add renumbering to all blocks that use their default names.  (First I'll need to fix default names.)
//
//TODO: FEATURE - finish PowerSwitch
//
//TODO: FEATURE - Rich wanted something in ConditionalActivator.cs ?
//