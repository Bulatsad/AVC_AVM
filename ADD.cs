using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AVC;

namespace AVI
{
    class ADD : IAsseblerVirtualModule
    {
        private Dictionary<string, byte> BaitCodeList;
        private Dictionary<string, byte> RegisterCodes;
        private Dictionary<string, int> RegisterSizes;
        private Dictionary<string, string> Flags;
        private Dictionary<string, byte> EBaitCodeList;
        private Dictionary<string, string> EFlags;
        private Dictionary<int, int> ERegSize;
        private Dictionary<string, int> ERegCode;
        private List<byte> ADDRR(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["addrr"]);
            result.Add(RegisterCodes[to]);
            result.Add(RegisterCodes[from]);
            return result;
        }
        private List<byte> ADDRM(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["addrm"]);
            result.Add(RegisterCodes[to]);
            result.Add(RegisterCodes[from.Substring(1, from.Length - 2)]);
            return result;
        }
        private List<byte> ADDRC(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["addrc"]);
            result.Add(RegisterCodes[to]);
            result.AddRange(Commands.ConvertToByte(Convert.ToInt64(from), RegisterSizes[to]));
            return result;
        }
        public byte[] Compile(string instruction)
        {
            string[] args = Commands.GetArguments(instruction);
            List<byte> binaryinst = null;
            byte rubbish;
            if (RegisterCodes.TryGetValue(args[1], out rubbish)) binaryinst = ADDRR(args[0], args[1]);
            else if (args[1][0] == '[' && args[1][args[1].Length - 1] == ']') binaryinst = ADDRM(args[0], args[1]);
            else binaryinst = ADDRC(args[0], args[1]);
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
            return instruction == "add";
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
        private int ReadReg(string reg, byte[] Registers)
        {
            List<byte> byteip = new List<byte>();
            for (int i = 0; i < ERegSize[ERegCode[reg]]; i++)
                byteip.Add(Registers[ERegCode[reg] + i]);
            if (byteip.Count == 1)
                return Convert.ToInt32(byteip[0]);
            return BitConverter.ToInt32(Commands.CompleteToLenght(byteip.ToArray(), 4), 0);
        }
        private int ReadReg(int addr, byte[] Registers)
        {
            List<byte> byteip = new List<byte>();
            for (int i = 0; i < ERegSize[addr]; i++)
                byteip.Add(Registers[addr + i]);
            return BitConverter.ToInt32(Commands.CompleteToLenght(byteip.ToArray(), 4), 0);
        }
        private void WriteReg(int val, string reg, ref byte[] Registers)
        {
            byte[] newip = BitConverter.GetBytes(val);
            for (int i = 0; i < ERegSize[ERegCode[reg]]; i++)
                Registers[ERegCode[reg] + i] = newip[i];
        }
        private void WriteReg(int val, int addr, ref byte[] Registers)
        {
            byte[] newip = BitConverter.GetBytes(val);
            for (int i = 0; i < ERegSize[addr]; i++)
                Registers[addr + i] = newip[i];
        }
        private int ReadMem(int addr,int bytecount, ref byte[] RAM)
        {
            List<byte> result = new List<byte>();
            for (int i = 0; i < bytecount; i++)
                result.Add(RAM[addr + i]);
            return BitConverter.ToInt32(Commands.CompleteToLenght(result.ToArray(), 4), 0);
        }
        public bool IsExecutable(byte baitCode)
        {
            return baitCode == EBaitCodeList["addrr"] ||
                baitCode == EBaitCodeList["addrm"] ||
                baitCode == EBaitCodeList["addrc"];
        }
        private void EADDRR(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = ReadReg("ip", Registers);
            int to = RAM[ip + 1];
            int from = RAM[ip + 2];
            int toval = ReadReg(to, Registers);
            int fromval = ReadReg(from, Registers);
            WriteReg(toval + fromval, to, ref Registers);
            ip += 3;
            WriteReg(ip, "ip", ref Registers);
        }
        private void EADDRM(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = ReadReg("ip", Registers);
            int to = RAM[ip + 1];
            int fromaddr = ReadReg(RAM[ip + 2], Registers);
            if (ERegSize[RAM[ip + 2]] != (Convert.ToInt32(EFlags["bit_depth"]) / 8))
                throw new Exception("insufficient address length");
            int toval = ReadReg(to, Registers);
            int fromval = ReadMem(fromaddr, ERegSize[to], ref RAM);
            WriteReg(toval + fromval, to, ref Registers);
            ip += 3;
            WriteReg(ip, "ip", ref Registers);
        }
        private void EADDRC(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = ReadReg("ip", Registers);
            int to = RAM[ip + 1];
            int toval = ReadReg(to, Registers);
            int fromval = ReadMem(ip + 2, ERegSize[to], ref RAM);
            WriteReg(toval + fromval, to, ref Registers);
            ip += 2 + ERegSize[to];
            WriteReg(ip, "ip", ref Registers);
        }
        public void Execute(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = ReadReg("ip", Registers);
            byte instruction = RAM[ip];
            if (instruction == EBaitCodeList["addrr"]) EADDRR(ref Registers, ref RAM);
            else if (instruction == EBaitCodeList["addrm"]) EADDRM(ref Registers, ref RAM);
            else if (instruction == EBaitCodeList["addrc"]) EADDRC(ref Registers, ref RAM);
        }

        public void InitExecute(Dictionary<string, byte> EBaitCodeList, Dictionary<string, int> ERegCode, Dictionary<int, int> ERegSize, Dictionary<string, string> EFlags)
        {
            this.EBaitCodeList = EBaitCodeList;
            this.ERegCode = ERegCode;
            this.ERegSize = ERegSize;
            this.EFlags = EFlags;
        }
    }
}