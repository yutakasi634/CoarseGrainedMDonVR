using System;
using System.Collections.Generic;
using UnityEngine;
using Nett;

namespace Coral_iMD
{
    internal class InputToml
    {
        private TomlTable m_SystemTable;

        internal InputToml(string input_file_path)
        {
            TomlTable root = Toml.ReadFile(input_file_path);
            List<TomlTable> systems = root.Get<List<TomlTable>>("systems");
            if (2 <= systems.Count)
            {
                throw new System.Exception(
                        $"There are {systems.Count} systems. the multiple systems case is not supported");
            }
            m_SystemTable = systems[0];
        }
    }
}
