using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AVI;

namespace AVM
{
    public class AssemblerVirtualMachine
    {
        private IAsseblerVirtualModule[] ModuleList;
        private Dictionary<string, byte> BaitCodeList;
        private Dictionary<int, int> RegisterSizes;
        private Dictionary<string, int> RegisterCodes;
        private Dictionary<string, string> Flags;
        private List<byte> RAM_ = new List<byte>();
        private List<byte> Registers_ = new List<byte>();
        private byte[] RAM;
        private byte[] Registers;
        private void LoadRegisterDefs(string path = "eregisterdefs.arc")
        {
            RegisterCodes = new Dictionary<string, int>();
            RegisterSizes = new Dictionary<int, int>();
            StreamReader reader = new StreamReader(path);
            int maxaddr = 0;
            int maxsize = 0;
            while (!reader.EndOfStream)
            {
                string command = reader.ReadLine();
                string[] args = VMCommands.GetArguments(command);
                if(Convert.ToInt32(args[0], 16) > maxaddr)
                {
                    maxaddr = Convert.ToInt32(args[0], 16);
                    maxsize = Convert.ToInt32(args[1], 10);
                }
                RegisterCodes.Add(command.Substring(0, command.IndexOf(' ')), Convert.ToInt32(args[0], 16));
                RegisterSizes.Add(Convert.ToInt32(args[0], 16), Convert.ToInt32(args[1], 10));
            }
            Registers_.Capacity = maxaddr + maxsize;
        }
        private void LoadBaitCodes(string path = "einstructiondefs.arc")
        {
            BaitCodeList = new Dictionary<string, byte>();
            StreamReader reader = new StreamReader(path);
            int i = 0;
            string instruction;
            while (!reader.EndOfStream)
            {
                instruction = reader.ReadLine();
                VMCommands.ClearCommand(ref instruction);
                BaitCodeList.Add(instruction, Convert.ToByte(i));
                i++;
            }
        }
        private void LoadFlags(string path = "eflags.arc")
        {
            Flags = new Dictionary<string, string>();
            StreamReader reader = new StreamReader(path);
            string line, command;
            string[] args;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                command = line.Substring(0, line.IndexOf(' '));
                args = VMCommands.GetArguments(line);
                switch (command)
                {
                    case "bit_depth": Flags[command] = args[0];break;
                    case "RAM_SIZE": Flags[command] = args[0];break;
                }
            }
        }
        public AssemblerVirtualMachine()
        {
            LoadBaitCodes();
            LoadRegisterDefs();
            LoadFlags();
            ModuleList = Connector.GetConnectedModudels();
            RAM_.Capacity = Convert.ToInt32(Flags["RAM_SIZE"]);
            for (int i = 0; i < RAM_.Capacity; i++)
                RAM_.Add(0);
            for (int i = 0; i < Registers_.Capacity; i++)
                Registers_.Add(0);
            RAM = RAM_.ToArray();
            Registers = Registers_.ToArray();
            foreach (IAsseblerVirtualModule module in ModuleList)
            {
                module.InitExecute(BaitCodeList, RegisterCodes, RegisterSizes, Flags);
            }

        }
        private void LoadProgramToMemory(byte[] program)
        {
            for (int i = 0; i < program.Length; i++)
                RAM[i] = program[i];
        }
        private int ReadReg(int regAddr,int regSize)
        {
            List<byte> byteResult = new List<byte>();
            for (int i = 0; i < regSize; i++)
                byteResult.Add(Registers[regAddr + i]);
            return BitConverter.ToInt32(byteResult.ToArray(), 0);
        }
        public byte Run(byte[] program_)
        {
            byte[] program = program_;
            LoadProgramToMemory(program);
            
            while(ReadReg(RegisterCodes["ip"],RegisterSizes[RegisterCodes["ip"]]) < program.Length)
            {
                foreach (IAsseblerVirtualModule module in ModuleList)
                {
                    if(module.IsExecutable(RAM[ReadReg(RegisterCodes["ip"], RegisterSizes[RegisterCodes["ip"]])]))
                    {
                        module.Execute(ref Registers, ref  RAM);
                    }
                }
            }


            return 0;
        }
    }
}
namespace AVC
{
    public class AVComplier
    {
        private static Dictionary<string, byte> BaitCodeList;
        private static Dictionary<string, byte> RegisterCodes;
        private static Dictionary<string, int> RegisterSizes;
        private static Dictionary<string, string> Flags;
        private static Dictionary<string, int> PointerList;
        private static IAsseblerVirtualModule[] ModuleList;
        private static bool IsPointer(string command)
        {
            if (command.Length < 2)
                return false;
            Commands.ClearCommand(ref command);
            return command[command.Length - 1] == ':';
        }
        private static List<byte> Binary;
        private static void LoadRegisterInfo(string path = "registerdefs.arc")
        {
            RegisterCodes = new Dictionary<string, byte>();
            RegisterSizes = new Dictionary<string, int>();
            StreamReader reader = new StreamReader(path);
            while (!reader.EndOfStream)
            {
                string command = reader.ReadLine();
                string[] args = Commands.GetArguments(command);
                RegisterCodes.Add(command.Substring(0, command.IndexOf(' ')), Convert.ToByte(args[0], 16));
                RegisterSizes.Add(command.Substring(0, command.IndexOf(' ')), Convert.ToInt16(args[1], 10));
            }
        }
        private static Dictionary<string, byte> LoadBaitCodes(string path = "instructiondefs.arc")
        {
            Dictionary<string, byte> result = new Dictionary<string, byte>();
            StreamReader reader = new StreamReader(path);
            int i = 0;
            string instruction;
            while (!reader.EndOfStream)
            {
                instruction = reader.ReadLine();
                Commands.ClearCommand(ref instruction);
                result.Add(instruction, Convert.ToByte(i));
                i++;
            }
            return result;
        }
        private static string[] CorrectCode(string[] code)
        {
            List<string> result = new List<string>();
            for(int i = 0;i<code.Length;i++)
            {
                string tmp = code[i];
                int pos = tmp.IndexOf('#');
                if (pos > 0)
                    tmp = tmp.Substring(0, pos);
                pos = tmp.IndexOf('\t');
                if (pos > 0)
                    tmp = tmp.Substring(0, pos);
                if (tmp.Length == 0)
                    continue;
                result.Add(tmp);
            }
            return result.ToArray();
        }
        private static void InitModules(IAsseblerVirtualModule[] moduleList)
        {
            foreach (IAsseblerVirtualModule module in moduleList)
                module.Init(RegisterCodes, RegisterSizes, BaitCodeList, Flags);
        }
        private static void LoadFlags(string path = "flags.arc")
        {
            Flags = new Dictionary<string, string>();
            StreamReader reader = new StreamReader(path);
            string line, command;
            string[] args;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                command = line.Substring(0, line.IndexOf(' '));
                args = Commands.GetArguments(line);
                switch (command)
                {
                    case "target_bit_depth": Flags[command] = args[0]; break;
                }
            }
        }
        public static byte[] Compile(string[] code_)
        {
            code_ = CorrectCode(code_);
            LoadFlags();
            Binary = new List<byte>();
            PointerList = new Dictionary<string, int>();
            BaitCodeList = LoadBaitCodes();
            LoadRegisterInfo();
            ModuleList = Connector.GetConnectedModudels();
            InitModules(ModuleList);
            string[] Code = code_;
            string Command;
            for (int i = 0; i < Code.Length; i++)
            {
                string Instruction = Code[i];
                if (IsPointer(Instruction))
                {
                    Commands.ClearCommand(ref Instruction);
                    PointerList.Add(Instruction.Substring(0, Instruction.Length - 1), Binary.Count);
                    if (i + 1 == Code.Length)
                        throw new Exception("No instruction after marker");
                    continue;
                }
                Command = Instruction.Substring(0, Instruction.IndexOf(' '));
                foreach (IAsseblerVirtualModule module in ModuleList)
                {
                    if (module.IsRealised(Command))
                    {
                        if (module.IsLinkable())
                        {
                            string[] args = Commands.GetArguments(Instruction);
                            module.InitLink(args[0], Binary.Count + 1);
                        }
                        Binary.AddRange(module.Compile(Instruction));

                    }
                }
            }
            foreach (IAsseblerVirtualModule module in ModuleList)
            {
                if (module.IsLinkable())
                    module.Link(PointerList, Binary);
            }
            foreach (byte a in Binary)
                Console.WriteLine(a);
            return Binary.ToArray();
        }
    }
}
