using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AVC;
using AVM;

namespace AVI
{
    class SUB : IAsseblerVirtualModule
    {
        private Dictionary<string, byte> BaitCodeList;
        private Dictionary<string, byte> RegisterCodes;
        private Dictionary<string, int> RegisterSizes;
        private Dictionary<string, string> Flags;
        private Dictionary<string, byte> EBaitCodeList;
        private Dictionary<string, string> EFlags;
        private Dictionary<int, int> ERegSize;
        private Dictionary<string, int> ERegCode;
        private List<byte> SUBRR(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["subrr"]);
            result.Add(RegisterCodes[to]);
            result.Add(RegisterCodes[from]);
            return result;
        }
        private List<byte> SUBRM(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["subrm"]);
            result.Add(RegisterCodes[to]);
            result.Add(RegisterCodes[from.Substring(1, from.Length - 2)]);
            return result;
        }
        private List<byte> SUBRC(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["subrc"]);
            result.Add(RegisterCodes[to]);
            result.AddRange(Commands.ConvertToByte(Convert.ToInt64(from), RegisterSizes[to]));
            return result;
        }
        public byte[] Compile(string instruction)
        {
            string[] args = Commands.GetArguments(instruction);
            List<byte> binaryinst = null;
            byte rubbish;
            if (RegisterCodes.TryGetValue(args[1], out rubbish)) binaryinst = SUBRR(args[0], args[1]);
            else if (args[1][0] == '[' && args[1][args[1].Length - 1] == ']') binaryinst = SUBRM(args[0], args[1]);
            else binaryinst = SUBRC(args[0], args[1]);
            return binaryinst.ToArray();
        }     
        public void Init(Dictionary<string, byte> RegisterCodes_, Dictionary<string, int> RegisterSizes_, Dictionary<string, byte> BaitCodeList_, Dictionary<string, string> Flags_)
        {
            RegisterCodes = RegisterCodes_;
            RegisterSizes = RegisterSizes_;
            BaitCodeList = BaitCodeList_;
            Flags = Flags_;
        }
        public bool IsRealised(string instruction)
        {
            return instruction == "sub";
        }
        public void InitLink(string pointer, int address)
        {
            throw new NotImplementedException();
        }
        public void Link(Dictionary<string, int> PointerList, List<byte> Binary)
        {
            throw new NotImplementedException();
        }
        public bool IsLinkable()
        {
            return false;
        }
        public bool IsExecutable(byte baitCode)
        {
            return baitCode == EBaitCodeList["subrr"] ||
                baitCode == EBaitCodeList["subrm"] ||
                baitCode == EBaitCodeList["subrc"];
        }
        private void ESUBRR(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg("ip", Registers);
            int to = RAM[ip + 1];
            int from = RAM[ip + 2];
            int toval = RM.ReadReg(to, Registers);
            int fromval = RM.ReadReg(from, Registers);
            RM.WriteReg(toval - fromval, to, ref Registers);
            ip += 3;
            RM.WriteReg(ip, "ip", ref Registers);
        }
        private void ESUBRM(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg("ip", Registers);
            int to = RAM[ip + 1];
            int fromaddr = RM.ReadReg(RAM[ip + 2], Registers);
            if (ERegSize[(int)RAM[ip + 2]] != (Convert.ToInt32(EFlags["bit_depth"]) / 8))
                throw new Exception("insufficient address length");
            int toval = RM.ReadReg(to, Registers);
            int fromval = RM.ReadMem(fromaddr, ERegSize[to], ref RAM);
            RM.WriteReg(toval - fromval, to, ref Registers);
            ip += 3;
            RM.WriteReg(ip, "ip", ref Registers);
        }
        private void ESUBRC(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg("ip", Registers);
            int to = RAM[ip + 1];
            int toval = RM.ReadReg(to, Registers);
            int fromval = RM.ReadMem(ip + 2, ERegSize[to], ref RAM);
            RM.WriteReg(toval - fromval, to, ref Registers);
            ip += 2 + ERegSize[to];
            RM.WriteReg(ip, "ip", ref Registers);
        }
        public void Execute(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg("ip", Registers);
            byte instruction = RAM[ip];
            if (instruction == EBaitCodeList["subrr"]) ESUBRR(ref Registers, ref RAM);
            else if (instruction == EBaitCodeList["subrm"]) ESUBRM(ref Registers, ref RAM);
            else if (instruction == EBaitCodeList["subrc"]) ESUBRC(ref Registers, ref RAM);
        }

        public void InitExecute(Dictionary<string, byte> EBaitCodeList, Dictionary<string, int> ERegCode, Dictionary<int, int> ERegSize, Dictionary<string, string> EFlags)
        {        
            this.EBaitCodeList = EBaitCodeList;
            this.ERegCode = ERegCode;
            this.ERegSize = ERegSize;
            this.EFlags = EFlags;
            RM.Init(ERegCode, ERegSize);
        }
    }
}