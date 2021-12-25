﻿using Caliburn.Micro;
using OngekiFumenEditor.Base;
using OngekiFumenEditor.Utils;
using OngekiFumenEditor.Utils.ObjectPool;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OngekiFumenEditor.Parser
{
    [Export(typeof(IOngekiFumenParser))]
    public class DefaultOngekiFumenParser : IOngekiFumenParser
    {
        [ImportMany]
        public IEnumerable<ICommandParser> CommandParsers { get; private set; }

        public async Task<OngekiFumen> ParseAsync(Stream stream)
        {
            var reader = new StreamReader(stream);
            var genObjList = new List<(OngekiObjectBase obj,ICommandParser parser)>();
            var fumen = new OngekiFumen();

            var commandArg = ObjectPool<CommandArgs>.Get();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                commandArg.Line = line;

                var cmdName = commandArg.GetData<string>(0)?.Trim();
                if (cmdName != null && CommandParsers.FirstOrDefault(x=> cmdName.Equals(x.CommandLineHeader,StringComparison.OrdinalIgnoreCase)) is ICommandParser parser)
                {
                    if (parser.Parse(commandArg, fumen) is OngekiObjectBase obj)
                    {
                        genObjList.Add((obj,parser));
                        fumen.AddObject(obj);
                    }
                }
            }

            ObjectPool<CommandArgs>.Return(commandArg);

            foreach (var pair in genObjList)
            {
                pair.parser.AfterParse(pair.obj, fumen);
            }

            fumen.Setup();

            return fumen;
        }
    }
}