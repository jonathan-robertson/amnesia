using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlyThreeChances.Commands {
    internal class ConsoleCmdOnlyThreeChances : ConsoleCmdAbstract {
        private static readonly string[] Commands = new string[] {
            "onlyThreeChances",
            "otc"
        };

        public override string[] GetCommands() {
            return Commands;
        }

        public override string GetDescription() {
            return "Configure or adjust settings for the Only Three Chances mod.";
        }

        public override string GetHelp() {
            int i = 1;
            int j = 1;
            return $@"Usage:
  {i++}. {GetCommands()[0]}
  {i++}. {GetCommands()[0]} TODO
  {i++}. {GetCommands()[0]} TODO
  {i++}. {GetCommands()[0]} TODO
Description Overview
{j++}. TODO
{j++}. TODO
{j++}. TODO
{j++}. TODO";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
            throw new NotImplementedException();
        }
    }
}
