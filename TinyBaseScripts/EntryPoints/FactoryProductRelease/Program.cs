using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript.EntryPoints.FactoryProductRelease
{
    class Program : MyGridProgram
    {
        static readonly string projectorsToCheckSubstring = "[PixieFactoryProductProjector]";
        static readonly string targetMergeBlocksSubstring = "[PixieFactoryProductRelease]";
        static readonly string outputLcdSubstring = "[PixieFactoryDisplay]";
        static readonly int outputLcdScreenIndex = 0; //TODO: Replace this with some substring permutation on outputLcdSubstring.  Maybe.  Or maybe use a struct or something to hold both the name and the index?



        static StringBuilder logBuilder;
        List<IMyShipMergeBlock> mergeBlocksToConsiderDisengaging;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateType)
        {
            try
            {
                logBuilder = new StringBuilder();

                mergeBlocksToConsiderDisengaging = GetProductAssemblyMergeBlocks();

                if (mergeBlocksToConsiderDisengaging.Any(mergeBlock => !mergeBlock.Enabled))
                {
                    EnableProductAttachment();
                }
                else
                {
                    ReleaseProductIfComplete();
                }

                OutputLog();
                return;
            }
            catch (Exception ex)
            {
                HandleUnexpectedException(ex);
            }
        }

        private void EnableProductAttachment()
        {
            List<IMyShipMergeBlock> mergeBlocksToConsiderDisengaging = GetProductAssemblyMergeBlocks();

            logBuilder.AppendLine("Turning merge blocks back on to weld the new products onto this run.");

            foreach (IMyShipMergeBlock mergeBlock in mergeBlocksToConsiderDisengaging)
            {
                ConnectMergeBlock(mergeBlock);
                logBuilder.AppendLine($"Reconnected merge block \"{mergeBlock.CustomName}\".");
            }
        }

        private void ReleaseProductIfComplete()
        {

            List<IMyProjector> projectorsToCheck = new List<IMyProjector>();
            GridTerminalSystem.GetBlocksOfType(projectorsToCheck, IsFactoryBlueprintProjector);

            if (!projectorsToCheck.Any())
            {
                logBuilder.AppendLine($"Error!  No projectors found with \"{projectorsToCheckSubstring}\" tag!  Aborting script.!");
                return;
            }

            logBuilder.AppendLine($"Checking {projectorsToCheck.Count()} projectors' blueprints for completeness.");
            bool areAllBlueprintsComplete = true;

            if (!projectorsToCheck.Any())
            {
                logBuilder.AppendLine("Error!  No projectors found!");
                return;
            }

            var projectorsWithIncompleteBlocks = projectorsToCheck.Where(IsProjectedBlueprintFullyPlaced);

            foreach (IMyProjector projector in projectorsWithIncompleteBlocks)
            {
                ReportIncompleteBlueprintAndFailCondition(projector, out areAllBlueprintsComplete);
            }

            if (!areAllBlueprintsComplete)
            {
                logBuilder.AppendLine($"Since some projectors are still constructing, the script will end.");
                return;
            }

            logBuilder.AppendLine($"All {projectorsToCheck.Count} projectors report completion.");

            IEnumerable<IMyShipMergeBlock> connectedMergeBlocks = mergeBlocksToConsiderDisengaging.Where(IsMergeBlockConnected);
            foreach (IMyShipMergeBlock mergeBlock in connectedMergeBlocks)
            {
                ReportConnectedMergeBlock(mergeBlock);
            }

            if (!connectedMergeBlocks.Any())
            {
                logBuilder.AppendLine($"None of the merge blocks tagged with \"{targetMergeBlocksSubstring}\" are Connected.  Ending script.");
                return;
            }

            IEnumerable<IMyShipMergeBlock> alreadyDisconnectedMergeBlocks = mergeBlocksToConsiderDisengaging
                .Where(blockToConsierDisengaging => connectedMergeBlocks.Contains(blockToConsierDisengaging));


            foreach (IMyShipMergeBlock mergeBlock in alreadyDisconnectedMergeBlocks)
            {
                ReportDisconnectedMergeBlock(mergeBlock);
            }

            IEnumerable<IMyShipMergeBlock> damagedMergeBlocks = connectedMergeBlocks.Where(CheckAndReportDamageToBlocksInGrid);
            if (damagedMergeBlocks.Any())
            {
                logBuilder.AppendLine($"Some blocks still need welding.  Ending script.");
                return;
            }

            foreach (IMyShipMergeBlock mergeBlock in connectedMergeBlocks)
            {
                DisconnectMergeBlock(mergeBlock);
                logBuilder.AppendLine($"Disconnected merge block {mergeBlock.Name}.");
            }

            logBuilder.AppendLine("Disconnect completed successfully, the next execution will reenable the merge block.");
            return;
        }
        private bool CheckAndReportDamageToBlocksInGrid(IMyShipMergeBlock mergeBlock)
        {
            List<IMyTerminalBlock> damagedBlocks = new List<IMyTerminalBlock>();
            this.GridTerminalSystem.GetBlocksOfType(damagedBlocks, block => 
                block.CubeGrid == mergeBlock.CubeGrid 
                && (
                    block.CubeGrid.GetCubeBlock(block.Position).CurrentDamage != 0
                    || block.CubeGrid.GetCubeBlock(block.Position).BuildLevelRatio < 1.0
                ));
            //foreach (var block in mergeBlock.CubeGrid
            //mergeBlock.CubeGrid.GetCubeBlock(damagedBlocks, slimBlock => slimBlock.CurrentDamage != 0);
            if (damagedBlocks.Any())
            {
                logBuilder.AppendLine($"There are {damagedBlocks.Count()} damaged blocks on the grid.  Execution will pause until they are repaired.");

                foreach (IMyTerminalBlock block in damagedBlocks)
                {
                    logBuilder.AppendLine($"\"{block.CustomName}\" is damaged.");
                }
                return true;
            }

            return false;
        }


        private List<IMyShipMergeBlock> GetProductAssemblyMergeBlocks()
        {
            List<IMyShipMergeBlock> productAssemblyMergeBlocks = new List<IMyShipMergeBlock>();
            GridTerminalSystem.GetBlocksOfType(productAssemblyMergeBlocks, IsFactoryProductReleaseMergeBlock);

            if (!productAssemblyMergeBlocks.Any())
            {
                throw new InvalidOperationException($"Error!  No merge blocks found with \"{targetMergeBlocksSubstring}\" tag!  Aborting script.");
            }
            return productAssemblyMergeBlocks;
        }

        private void OutputLog()
        {
            string builtLog = null;

            try
            {
                List<IMyTextSurface> outputSurfaces = new List<IMyTextSurface>();
                GridTerminalSystem.GetBlocksOfType(outputSurfaces, IsFactoryDisplayBlock);
                List<IMyTextSurfaceProvider> outputSurfaceProviders = new List<IMyTextSurfaceProvider>();
                GridTerminalSystem.GetBlocksOfType(outputSurfaceProviders, IsFactoryDisplayProviderBlock);

                outputSurfaces.AddRange(ProvideSurfacesOrThrowException(outputSurfaceProviders)
                );

                if (!outputSurfaces.Any())
                {
                    logBuilder.AppendLine($"No screen blocks found for the tag {outputLcdSubstring}");
                    return;
                }
                builtLog = logBuilder.ToString();

                outputSurfaces.ForEach(surface => DisplayLogOnSurface(surface, builtLog));
            }
            finally
            {
                if (builtLog == null)
                {
                    builtLog = logBuilder.ToString();
                }

                Echo(builtLog);
                builtLog = null;
            }
        }

        private void DisplayLogOnSurface(IMyTextSurface surface, string textToOutput)
        {
            surface.ContentType = ContentType.TEXT_AND_IMAGE;
            surface.WriteText(textToOutput);
        }

        private bool IsProjectedBlueprintFullyPlaced(IMyProjector projectorToCheck)
        {
            return projectorToCheck.RemainingBlocks != 0;
        }


        IMySlimBlock GetSlimBlockFromFat(IMyTerminalBlock block)
        {
            return block.CubeGrid.GetCubeBlock(block.Position);
        }

        private static void ReportIncompleteBlueprintAndFailCondition(IMyProjector projector, out bool areAllBlueprintsComplete)
        {
            logBuilder.AppendLine($"Projector {projector.CustomName} reports that its blueprint is not complete!");
            areAllBlueprintsComplete = false;
        }

        private bool IsFactoryDisplayProviderBlock(IMyTextSurfaceProvider provider)
        {
            return DoesBlockNameMatchSubstring(provider as IMyTerminalBlock, outputLcdSubstring);
        }

        private bool IsFactoryDisplayBlock(IMyTextSurface surfaceBlock)
        {
            return DoesBlockNameMatchSubstring(surfaceBlock as IMyTerminalBlock, outputLcdSubstring);
        }

        private bool IsFactoryBlueprintProjector(IMyProjector target)
        {
            return DoesBlockNameMatchSubstring(target, projectorsToCheckSubstring);
        }

        private bool IsFactoryProductReleaseMergeBlock(IMyShipMergeBlock target)
        {
            return DoesBlockNameMatchSubstring(target, targetMergeBlocksSubstring);
        }

        private bool DoesBlockNameMatchSubstring<TTerminalBlock>(TTerminalBlock target, string substring) where TTerminalBlock : IMyTerminalBlock
        {
            bool matches = target.CustomName.ToUpper().Contains(substring.ToUpper());
            return matches;
        }
        private bool IsMergeBlockConnected(IMyShipMergeBlock blockToCheck)
        {
            return blockToCheck.IsConnected;
        }

        private void ReportDisconnectedMergeBlock(IMyShipMergeBlock mergeBlockToReport)
        {
            logBuilder.AppendLine($"Merge block \"{mergeBlockToReport.CustomName}\" is currently Connected.");
        }

        private void ReportConnectedMergeBlock(IMyShipMergeBlock mergeBlockToReport)
        {
            logBuilder.AppendLine($"Merge block \"{mergeBlockToReport.CustomName}\" is currently Connected.");
        }
        private void ConnectMergeBlock(IMyShipMergeBlock mergeBlockToConnect)
        {
            mergeBlockToConnect.Enabled = true;
        }

        private void DisconnectMergeBlock(IMyShipMergeBlock mergeBlockToDisconnect)
        {
            mergeBlockToDisconnect.Enabled = false;
        }

        private IEnumerable<IMyTextSurface> ProvideSurfacesOrThrowException(List<IMyTextSurfaceProvider> providers)
        {
            return
                providers.Select
                (
                    provider =>
                    {
                        if (provider.SurfaceCount <= outputLcdScreenIndex || outputLcdScreenIndex < 0)
                        {
                            throw new InvalidOperationException($"Output LCD screen index is {outputLcdScreenIndex}, multi-screen block "
                                + $"\"{(provider as IMyTerminalBlock).CustomName}\" can only accept indices from 0 (first screen) up to "
                                + $" {provider.SurfaceCount - 1} (last screen)!  Aborting script.");
                        }
                        return provider.GetSurface(outputLcdScreenIndex);
                    }
                );
        }

        private void HandleUnexpectedException(Exception ex)
        {
            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }

            logBuilder.AppendLine()
                .AppendLine($"Unexpected {ex.GetType().Name}:")
                .AppendLine(ex.Message)
                .AppendLine("Stack trace:")
                .AppendLine()
                .AppendLine(ex.StackTrace);

            try
            {
                OutputLog();
            }
            catch (Exception outputException)
            {
                logBuilder.AppendLine("Failed to output log.  Falling back to Echo.  Output log exception:")
                .AppendLine()
                .AppendLine($"{outputException.GetType().Name}:")
                .AppendLine(outputException.Message)
                .AppendLine("Stack trace:")
                .AppendLine()
                .AppendLine(outputException.StackTrace); ;

                Echo(logBuilder.ToString());
            }
        }


    }
}
