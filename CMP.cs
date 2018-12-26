using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AVC;
using AVM;

namespace AVI
{
    class CMP : IAsseblerVirtualModule , ILinkable
    {
        private Dictionary<string, byte> BaitCodeList;
        private Dictionary<string, byte> RegisterCodes;
        private Dictionary<string, int> RegisterSizes;
        private Dictionary<string, string> Flags;
        private Dictionary<string, byte> EBaitCodeList;
        private Dictionary<string, string> EFlags;
        private Dictionary<int, int> ERegSize;
        private Dictionary<string, int> ERegCode;
        private byte[] CMPRR(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["cmprr"]);
            result.Add(RegisterCodes[args[0]]);
            result.Add(RegisterCodes[args[1]]);
            return result.ToArray();
        }
        private byte[] CMPRM(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["cmprm"]);
            result.Add(RegisterCodes[args[0]]);
            result.Add(RegisterCodes[args[1].Substring(1,args[1].Length -2)]);
            return result.ToArray();
        }
        private byte[] CMPRC(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["cmprc"]);
            result.Add(RegisterCodes[args[0]]);
            result.AddRange(Commands.CompleteToLenght(Commands.ConvertToByte(Convert.ToInt64(args[1]),8),RegisterSizes[args[0]]));
            return result.ToArray();
        }
        private byte[] CMPMR(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["cmpmr"]);
            result.Add(RegisterCodes[args[0].Substring(1, args[0].Length - 2)]);
            result.Add(RegisterCodes[args[1]]);
            return result.ToArray();
        }
        private byte[] CMPXX(string instruction,string[] args)
        {
            if (Commands.IsRegister(args[0], RegisterCodes) &&
                Commands.IsRegister(args[1], RegisterCodes))
                return CMPRR(args);
            if (Commands.IsRegister(args[0], RegisterCodes) &&
                Commands.IsConstant(args[1], RegisterCodes))
                return CMPRC(args);
            if (Commands.IsRegister(args[0], RegisterCodes) &&
                Commands.IsMemory(args[1], RegisterCodes)) 
                return CMPRM(args);
            if (Commands.IsMemory(args[0], RegisterCodes) &&
                Commands.IsRegister(args[1], RegisterCodes))
                return CMPMR(args);
            return null;
        }
       
        public byte[] Compile(string instruction)
        {
            string[] args = Commands.GetArguments(instruction);
            string command = instruction.Substring(0, instruction.IndexOf(' '));
            return CMPXX(command, args);
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
            return
                instruction == "cmp";
        }

        public void InitLink(string pointer, int address)
        {
            throw new Exception();
        }

        public void Link(Dictionary<string, int> PointerList, List<byte> Binary)
        {
            throw new Exception();
        }
        public bool IsLinkable()
        {
            return false;
        }
        public bool IsExecutable(byte baitCode)
        {
            return baitCode == EBaitCodeList["cmprr"] ||
                baitCode == EBaitCodeList["cmprm"] ||
                baitCode == EBaitCodeList["cmprc"] ||
                baitCode == EBaitCodeList["cmpmr"];
        }

        private int ECMPRR(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg(ERegCode["ip"], Registers);
            int a = RM.ReadReg(RAM[ip + 1], Registers);
            int b = RM.ReadReg(RAM[ip + 2], Registers);
            ip += 3;
            RM.WriteReg(ip, ERegCode["ip"], ref Registers);
            if (a < b)
                return -1;
            else if (a == b)
                return 0;
            else if (a > b)
                return 1;
            return -100;
        }
        private int ECMPRM(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg(ERegCode["ip"], Registers);
            int aval = RM.ReadReg(RAM[ip + 1], Registers);
            int baddr = RM.ReadReg(RAM[ip + 2], Registers);
            int bval = RM.ReadMem(baddr, ERegSize[ip + 2], ref RAM);
            ip += 3;
            RM.WriteReg(ip, ERegCode["ip"], ref Registers);
            if (aval < bval)
                return -1;
            else if (aval == bval)
                return 0;
            else if (aval > bval)
                return 1;
            return -100;
        }
        private int ECMPRC(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg(ERegCode["ip"], Registers);
            int aval = RM.ReadReg(RAM[ip + 1], Registers);
            int bval = RM.ReadMem(ip + 2, ERegSize[RAM[ip + 1]], ref RAM);
            ip += 2 + ERegSize[RAM[ip + 1]];
            RM.WriteReg(ip, ERegCode["ip"], ref Registers);
            if (aval < bval)
                return -1;
            else if (aval == bval)
                return 0;
            else if (aval > bval)
                return 1;
            return -100;
        }
        private int ECMPMR(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg(ERegCode["ip"], Registers);
            int bval = RM.ReadReg(RAM[ip + 1], Registers);
            int aaddr = RM.ReadReg(RAM[ip + 2], Registers);
            int aval = RM.ReadMem(aaddr, ERegSize[ip + 2], ref RAM);
            ip += 3;
            RM.WriteReg(ip, ERegCode["ip"], ref Registers);
            if (aval < bval)
                return -1;
            else if (aval == bval)
                return 0;
            else if (aval > bval)
                return 1;
            return -100;
        }

        public void Execute(ref byte[] Registers, ref byte[] RAM) // -1 a < b , 0 a = b, 1 a > b 
        {
            int ip = RM.ReadReg(ERegCode["ip"], Registers);
            int res = -99;
            if (RAM[ip] == EBaitCodeList["cmprr"]) res = ECMPRR(ref Registers, ref RAM);
            else if (RAM[ip] == EBaitCodeList["cmprm"]) res = ECMPRM(ref Registers, ref RAM);
            else if (RAM[ip] == EBaitCodeList["cmprc"]) res = ECMPRC(ref Registers, ref RAM);
            else if (RAM[ip] == EBaitCodeList["cmpmr"]) res = ECMPMR(ref Registers, ref RAM);
            if (res == -1)
            {
                RM.WriteReg(1, ERegCode["sf"], ref Registers);
                RM.WriteReg(0, ERegCode["zf"], ref Registers);
            }
            else if (res == 0)
            {
                RM.WriteReg(0, ERegCode["sf"], ref Registers);
                RM.WriteReg(1, ERegCode["zf"], ref Registers);
            }
            else if (res == 1)
            {
                RM.WriteReg(0, ERegCode["sf"], ref Registers);
                RM.WriteReg(0, ERegCode["zf"], ref Registers);
            }
            else
                throw new Exception("CMP module error");
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