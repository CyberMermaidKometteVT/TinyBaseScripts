using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript.EntryPoints.Doorman
{
    class Program : MyGridProgram
    {
        static readonly string targetMergeBlocksSubstring = "[PixieMannedDoor]";
        static readonly string outputLcdSubstring = "[PixieDoormanDisplay]";
        static readonly int outputLcdScreenIndex = 0; //TODO: Replace this with some substring permutation on outputLcdSubstring.  Maybe.  Or maybe use a struct or something to hold both the name and the index?

        bool SkipOtherGrids { get { return true; } }
        int ExecutionCountForDoorToRemainOpen { get { return 5; } }


        static StringBuilder logBuilder;
        Dictionary<IMyDoor, int> executionsSinceDoorWasOpened = new Dictionary<IMyDoor, int>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateType)
        {
            logBuilder = new StringBuilder();


            //Get all doors
            //For each door...
            // + ... If it's not in the dictionary, add it with a count of 0
            //  ... If it is in the dictionary and it's hit the limit, close it and set its count to 0
            // +  ... If it's closed, set its count to 0
            //  ... Otherwise, increment its count.
            //Output log
            try
            {

                List<IMyDoor> doors = new List<IMyDoor>();
                GridTerminalSystem.GetBlocksOfType(doors, door => DoesBlockNameMatchSubstring(door, targetMergeBlocksSubstring));

                if (SkipOtherGrids)
                {
                    doors.RemoveAll(door => door.CubeGrid != Me.CubeGrid);
                }

                logBuilder.AppendLine($"Found {doors.Count} managed doors.  We already knew about {executionsSinceDoorWasOpened.Keys.Count} doors.");

                foreach (IMyDoor door in doors)
                {
                    logBuilder.AppendLine($"Examining door {door.CustomName}.  ");

                    if (!executionsSinceDoorWasOpened.ContainsKey(door))
                    {
                        logBuilder.AppendLine($"It is new - beginning to track it.");
                        executionsSinceDoorWasOpened.Add(door, 0);
                    }
                    else if (door.Status == DoorStatus.Closed || door.Status == DoorStatus.Closing)
                    {
                        logBuilder.AppendLine($"It is not open, setting its time opened to 0.");
                        executionsSinceDoorWasOpened[door] = 0;
                    }
                    else
                    {
                        logBuilder.Append($"It has been open for {executionsSinceDoorWasOpened[door]}/{ExecutionCountForDoorToRemainOpen} executions.  ");

                        if (executionsSinceDoorWasOpened[door] >= ExecutionCountForDoorToRemainOpen)
                        {
                            logBuilder.AppendLine($"Closing it and setting its time opened to 0.");
                            door.CloseDoor();
                            executionsSinceDoorWasOpened[door] = 0;
                        }
                        else
                        {
                            logBuilder.AppendLine($"Incrementing its time opened.");
                            executionsSinceDoorWasOpened[door]++;
                        }
                    }
                }

                OutputLog();
                return;
            }
            catch (Exception ex)
            {
                HandleUnexpectedException(ex);
            }
        }


        private void OutputLog()
        {
            string builtLog = null;

            try
            {
                List<IMyTextSurface> outputSurfaces = new List<IMyTextSurface>();
                GridTerminalSystem.GetBlocksOfType(outputSurfaces, IsDisplayBlockForThisScript);
                List<IMyTextSurfaceProvider> outputSurfaceProviders = new List<IMyTextSurfaceProvider>();
                GridTerminalSystem.GetBlocksOfType(outputSurfaceProviders, IsDisplayProviderBlockForThisScript);

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

        private bool IsDisplayProviderBlockForThisScript(IMyTextSurfaceProvider provider)
        {
            return DoesBlockNameMatchSubstring(provider as IMyTerminalBlock, outputLcdSubstring);
        }

        private bool IsDisplayBlockForThisScript(IMyTextSurface surfaceBlock)
        {
            return DoesBlockNameMatchSubstring(surfaceBlock as IMyTerminalBlock, outputLcdSubstring);
        }
        private bool DoesBlockNameMatchSubstring<TTerminalBlock>(TTerminalBlock target, string substring) where TTerminalBlock : IMyTerminalBlock
        {
            bool matches = target.CustomName.ToUpper().Contains(substring.ToUpper());
            return matches;
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
