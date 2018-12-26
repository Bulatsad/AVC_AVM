using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AVC;
using AVM;
namespace AVI
{
    class DIV : IAsseblerVirtualModule
    {
        private Dictionary<string, byte> BaitCodeList;
        private Dictionary<string, byte> RegisterCodes;
        private Dictionary<string, int> RegisterSizes;
        private Dictionary<string, string> Flags;
        private Dictionary<string, byte> EBaitCodeList;
        private Dictionary<string, int> ERegCode;
        private Dictionary<int, int> ERegSize;
        private Dictionary<string, string> EFlags;
        private List<byte> DIVR(string to)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["divr"]);
            result.Add(RegisterCodes[to]);
            return result;
        }
        private List<byte> DIVM(string to)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["divm"]);
            result.Add(RegisterCodes[to.Substring(1, to.Length - 2)]);
            return result;
        }

        public byte[] Compile(string instruction)
        {
            string[] args = Commands.GetArguments(instruction);
            List<byte> binaryinst = null;
            byte rubbish;
            if (args[0][0] == '[' && args[0][args[0].Length - 1] == ']') binaryinst = DIVM(args[0]);
            else if (RegisterCodes.TryGetValue(args[0], out rubbish)) binaryinst = DIVR(args[0]);
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
            return instruction == "div";
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
            return baitCode == EBaitCodeList["divr"] ||
                baitCode == EBaitCodeList["divm"];
        }
        private void EMULR(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg("ip", Registers);
            int mulregaddr = RM.ReadMem(ip + 1, 1, ref RAM);
            int mulvalb = RM.ReadReg(mulregaddr, Registers);
            int mulvala = RM.ReadReg("eax", Registers);
            RM.WriteReg(mulvala / mulvalb, ERegCode["eax"], ref Registers);
            RM.WriteReg(ip + 2, ERegCode["ip"], ref Registers);

        }
        private void EMULM(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg("ip", Registers);
            int mulregaddr = RM.ReadMem(ip + 1, 1, ref RAM);
            int mulramaddr = RM.ReadReg(mulregaddr, Registers);
            int mulvalb = RM.ReadMem(mulramaddr, ERegSize[mulregaddr], ref RAM);
            int mulvala = RM.ReadReg("eax", Registers);
            RM.WriteReg(mulvala / mulvalb, ERegCode["eax"], ref Registers);
            RM.WriteReg(ip + 2, ERegCode["ip"], ref Registers);

        }

        public void Execute(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg("ip", Registers);
            byte code = RAM[ip];
            if (code == EBaitCodeList["divr"]) EMULR(ref Registers, ref RAM);
            else if (code == EBaitCodeList["divm"]) EMULM(ref Registers, ref RAM);
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