﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;
using quickup.Enums;

namespace quickup.Options
{
    /// <summary>
    /// The base <see langword="class"/> to hold the user options when invoking the program
    /// </summary>
    internal sealed class QuickupOptions
    {
        [Option('i', "include", HelpText = "The list of file extensions to look for when scanning the source directory. If not specified, all existing files will be copied.", Required = false, Separator = ',')]
        public IEnumerable<string> FileInclusions { get; set; }

        [Option('e', "exclude", HelpText = "The list of optional file extensions to ignore.", Required = false, Separator = ',')]
        public IEnumerable<string> FileExclusions { get; set; }

        [Option("ignore-dir", HelpText = "The list of optional subdirectories to ignore. Note that the dir name will be matched, not the relative path.", Required = false, Separator = ',')]
        public IEnumerable<string> DirExclusions { get; set; }

        [Option('p', "preset", Default = ExtensionsPreset.None, HelpText = "An optional preset to quickly filter certain common file types. This option cannot be used when --include or --exclude are used. Existing options are [documents|images|music|videos|code|vs].", Required = false)]
        public ExtensionsPreset Preset { get; set; }

        [Option("id", HelpText = "An optional id to keep multiple backups of the same source folder in the same directory.", Required = false)]
        public string Id { get; set; }

        [Option('M', "maxsize", Default = 104_857_600, HelpText = "The maximum size of files to be copied.", Required = false)]
        public long MaxSize { get; set; }

        [Option('s', "source", HelpText = "The source directory to backup.", Required = false)]
        public string SourceDirectory { get; set; }

        [Option("source-current", Default = false, HelpText = "Shortcut to set the source directory as the current working directory.", Required = false)]
        public bool SourceDirectoryCurrent { get; set; }

        [Option('t', "target", HelpText = "The target directory to use to store the backup.", Required = true)]
        public string TargetDirectory { get; set; }

        [Option('b', "beep", Default = false, HelpText = "Play a sound when the requested operation completes.", Required = false)]
        public bool Beep { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Indicates whether or not to display additional statistics.", Required = false)]
        public bool Verbose { get; set; }

        [Option("multithread", Default = false, HelpText = "Indicates whether or not to copy the files in parallel. This should be enabled when working on an SSD.", Required = false)]
        public bool Multithread { get; set; }

        [Option("threads", Default = -1, HelpText = "Indicates the maximum number of threads to use to copy the files. If set to -1, either 1 or the number of CPU threads will be used.", Required = false)]
        public int Threads { get; set; }

        /// <summary>
        /// Preprocesses the current options and applies the user settings
        /// </summary>
        public void Preprocess()
        {
            Id = string.IsNullOrEmpty(Id) ? string.Empty : $"_{Id}";
            if (Preset.TryExpand(out var info))
            {
                FileExclusions = info.Exclusions;
                DirExclusions = info.Directories;
            }
        }

        /// <summary>
        /// Executes a preliminary validation of the current instance
        /// </summary>
        [AssertionMethod]
        public void Validate()
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            if (FileInclusions.Any(ext => ext.Any(c => invalid.Contains(c)) || ext.Contains('.')))
                throw new ArgumentException("One or more file extensions are not valid");
            if (FileExclusions.Any(ext => ext.Any(c => invalid.Contains(c)) || ext.Contains('.')))
                throw new ArgumentException("One or more file extensions are not valid");
            invalid = Path.GetInvalidPathChars();
            if (DirExclusions.Any(ext => ext.Contains(Path.DirectorySeparatorChar) || ext.Any(c => invalid.Contains(c))))
                throw new ArgumentException("One or more dir exclusions are not valid");
            if (!string.IsNullOrEmpty(Id) && (Id.Contains(Path.DirectorySeparatorChar) || Id.Any(c => invalid.Contains(c))))
                throw new ArgumentException("The backup id is not valid");
            if (MaxSize <= 100) throw new ArgumentException("The maximum size must be at least 100KB");
            if (string.IsNullOrEmpty(SourceDirectory) && !SourceDirectoryCurrent) throw new ArgumentException("The source directory can't be empty");
            if (SourceDirectoryCurrent && !string.IsNullOrEmpty(SourceDirectory))
                throw new ArgumentException("The --source-current and --source options can't be used at the same time");
            if (!string.IsNullOrEmpty(SourceDirectory))
            {
                invalid = Path.GetInvalidPathChars();
                if (SourceDirectory.Any(c => invalid.Contains(c))) throw new ArgumentException("The source directory isn't valid");
                if (!Directory.Exists(SourceDirectory)) throw new ArgumentException("The source directory doesn't exist");
            }
            if (!Directory.Exists(TargetDirectory)) throw new ArgumentException("The target directory doesn't exist");
            if (FileInclusions.Any() && FileExclusions.Any())
                throw new ArgumentException("The list of extensions to exclude must be empty when other extensions to look for are specified");
            if (Preset != ExtensionsPreset.None && (FileInclusions.Any() || FileExclusions.Any()))
                throw new ArgumentException("The preset option cannot be used with --include or --exclude");
            if (Threads == 0 || Threads < -1) throw new ArgumentException("Invalid number of threads");
            if (Multithread && Threads == 1) throw new ArgumentException("Do you really want to multithread with just a single thread?");
        }
    }
}
