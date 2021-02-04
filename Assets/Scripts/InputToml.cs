using System;
using System.Collections.Generic;
using UnityEngine;
using Nett;

namespace Coral_iMD
{
    internal class InputToml
    {
        internal TomlTable SimulatorTable  { get; }
        internal TomlTable SystemTable     { get; }
        internal TomlTable ForceFieldTable { get; }

        internal InputToml(string input_file_path)
        {
            TomlTable root = Toml.ReadFile(input_file_path);

            SimulatorTable = root.Get<TomlTable>("simulator");

            List<TomlTable> systems = root.Get<List<TomlTable>>("systems");
            if (2 <= systems.Count)
            {
                throw new System.Exception(
                        $"There are {systems.Count} systems. the multiple systems case is not supported");
            }
            SystemTable = systems[0];

            List<TomlTable> forcefields = root.Get<List<TomlTable>>("forcefields");
            if (2 <= forcefields.Count)
            {
                throw new System.Exception(
                        $"There are {forcefields.Count} systems. the multiple systems case is not supported");
            }
            ForceFieldTable = forcefields[0];
        }
    }
}
