using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AVC;

namespace AVI
{
    class MOV : IAsseblerVirtualModule
    {
        private Dictionary<string, byte> BaitCodeList;
        private Dictionary<string, byte> RegisterCodes;
        private Dictionary<string, int> RegisterSizes;
        private Dictionary<string, string> Flags;
        private Dictionary<string, byte> EBaitCodeList;
        private Dictionary<string, string> EFlags;
        private Dictionary<int, int> ERegSize;
        private Dictionary<string, int> ERegCode;
        private List<byte> MOVRR(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["movrr"]);
            result.Add(RegisterCodes[to]);
            result.Add(RegisterCodes[from]);
            return result;
        }
        private List<byte> MOVRM(string to, string from) //адрес памяти берется из регистра 
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["movrm"]);
            result.Add(RegisterCodes[to]);
            result.Add(RegisterCodes[from.Substring(1, from.Length - 2)]);
            return result;
        }
        private List<byte> MOVRC(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["movrc"]);
            result.Add(RegisterCodes[to]);
            result.AddRange(Commands.ConvertToByte(Convert.ToInt64(from), RegisterSizes[to]));
            return result;
        }
        private List<byte> MOVMR(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["movmr"]);
            result.Add(RegisterCodes[to.Substring(1, to.Length - 2)]);
            result.Add(RegisterCodes[from]);
            return result;
        }
        private List<byte> MOVMC(string to, string from)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["movmc"]);
            result.Add(RegisterCodes[to.Substring(1, to.Length - 2)]);
            result.AddRange(Commands.ConvertToByte(Convert.ToInt64(from), RegisterSizes[to.Substring(1, to.Length - 2)]));
            return result;
        }
        private int ReadReg(string reg, byte[] Registers)
        {
            List<byte> byteip = new List<byte>();
            for (int i = 0; i < ERegSize[ERegCode[reg]]; i++)
                byteip.Add(Registers[ERegCode[reg] + i]);
            if (byteip.Count == 1)
                return Convert.ToInt32(byteip[0]);
            return BitConverter.ToInt32(byteip.ToArray(), 0);
        }
        private int ReadReg(int addr, byte[] Registers)
        {
            List<byte> byteip = new List<byte>();
            for (int i = 0; i < ERegSize[addr]; i++)
                byteip.Add(Registers[addr + i]);
            return BitConverter.ToInt32(Commands.CompleteToLenght(byteip.ToArray(),4),0);
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
        private int ReadMem(int addr, int bytecount, ref byte[] RAM)
        {
            List<byte> result = new List<byte>();
            for (int i = 0; i < bytecount; i++)
                result.Add(RAM[addr + i]);
            return BitConverter.ToInt32(result.ToArray(), 0);
        }
        private void EMOVRR(ref byte[] Registers,ref byte[] RAM)
        {
            List<byte> oldip = new List<byte>();
            for (int i = 0; i < ERegSize[ERegCode["ip"]]; i++)
                oldip.Add(Registers[ERegCode["ip"] + i]);
            int ip = BitConverter.ToInt32(oldip.ToArray(), 0);
            int to = Convert.ToInt32(RAM[ip + 1]);
            int from = Convert.ToInt32(RAM[ip + 2]);
            for (int i = 0; i < ERegSize[to]; i++)
                Registers[to + i] = Registers[from + i];
            ip += 3;
            byte[] newip = BitConverter.GetBytes(ip);
            for (int i = 0; i < ERegSize[ERegCode["ip"]]; i++)
                Registers[ERegCode["ip"] + i] = newip[i];
        }
        private void EMOVRM(ref byte[] Registers, ref byte[] RAM)
        {
            List<byte> oldip = new List<byte>();
            for (int i = 0; i < ERegSize[ERegCode["ip"]]; i++)
                oldip.Add(Registers[ERegCode["ip"] + i]);
            int ip = BitConverter.ToInt32(oldip.ToArray(), 0);
            int to = Convert.ToInt32(RAM[ip + 1]);
            int from = Convert.ToInt32(RAM[ip + 2]);
            if (ERegSize[from] != (Convert.ToInt32(EFlags["bit_depth"])  /8))
                throw new Exception("insufficient address length");
            List<byte> bytefrom = new List<byte>();
            for (int i = 0; i < ERegSize[to]; i++)
                bytefrom.Add(Registers[from + i]);
            int fromaddr = BitConverter.ToInt32(bytefrom.ToArray(), 0);
            for (int i = 0; i < ERegSize[to]; i++)
                Registers[to + i] = RAM[fromaddr + i];
            ip += 3;
            byte[] newip = BitConverter.GetBytes(ip);
            for (int i = 0; i < ERegSize[ERegCode["ip"]]; i++)
                Registers[ERegCode["ip"] + i] = newip[i];

        }
        private void EMOVRC(ref byte[] Registers, ref byte[] RAM)
        {
            List<byte> oldip = new List<byte>();
            for (int i = 0; i < ERegSize[ERegCode["ip"]]; i++)
                oldip.Add(Registers[ERegCode["ip"] + i]);
            int ip = BitConverter.ToInt32(oldip.ToArray(), 0);
            int to = Convert.ToInt32(RAM[ip + 1]);
            for (int i = 0; i < ERegSize[to]; i++)
                Registers[to + i] = RAM[ip + 2 + i];
            ip += 2 + ERegSize[to];
            byte[] newip = BitConverter.GetBytes(ip);
            for (int i = 0; i < ERegSize[ERegCode["ip"]]; i++)
                Registers[ERegCode["ip"] + i] = newip[i];
        }
        private void EMOVMR(ref byte[] Registers, ref byte[] RAM)
        {
             List<byte> oldip = new List<byte>();
            for (int i = 0; i < ERegSize[ERegCode["ip"]]; i++)
                oldip.Add(Registers[ERegCode["ip"] + i]);
            int ip = BitConverter.ToInt32(oldip.ToArray(), 0);
            int to = RAM[ip + 1];
            int from = RAM[ip + 2];

            List<byte> bytetoaddr = new List<byte>();
            for (int i = 0; i < ERegSize[to]; i++)
                bytetoaddr.Add(Registers[to + i]);
            int toaddr = BitConverter.ToInt32(bytetoaddr.ToArray(), 0);
            for (int i = 0; i < ERegSize[to]; i++)
                RAM[toaddr + i] = Registers[from + i];
            ip += 3;
            byte[] newip = BitConverter.GetBytes(ip);
            for (int i = 0; i < ERegSize[ERegCode["ip"]]; i++)
                Registers[ERegCode["ip"] + i] = newip[i];

        }
        private void EMOVMC(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = ReadReg("ip", Registers);
            int to = RAM[ip + 1];
            if (ERegSize[to] != (Convert.ToInt32(EFlags["bit_depth"]) / 8))
                throw new Exception("insufficient address length");
            int toaddr = ReadReg(to, Registers);
            for (int i = 0; i < ERegSize[to]; i++)
                RAM[toaddr + i] = RAM[ip + 2 + i];
            ip += 2 + ERegSize[to];
            WriteReg(ip, "ip", ref Registers);
        }
        public byte[] Compile(string instruction)
        {
            string[] args = Commands.GetArguments(instruction);
            List<byte> binaryinst = null;
            byte rubbish;
            if (args[0][0] == '[' && args[0][args[0].Length - 1] == ']' && RegisterCodes.TryGetValue(args[1], out rubbish)) binaryinst = MOVMR(args[0], args[1]);
            else if (args[0][0] == '[' && args[0][args[0].Length - 1] == ']') binaryinst = MOVMC(args[0], args[1]);
            else if (RegisterCodes.TryGetValue(args[1], out rubbish)) binaryinst = MOVRR(args[0], args[1]);
            else if (args[1][0] == '[' && args[1][args[1].Length - 1] == ']') binaryinst = MOVRM(args[0], args[1]);
            else binaryinst = MOVRC(args[0], args[1]);
            return binaryinst.ToArray();
        }
        public void Execute(ref byte[] Registers,ref byte[] RAM)
        {
            List<byte> byteip = new List<byte>();
            for (int i = 0; i < ERegSize[ERegCode["ip"]]; i++)
                byteip.Add(Registers[ERegCode["ip"] + i]);
            int ip = BitConverter.ToInt32(byteip.ToArray(), 0);
            byte instruction = RAM[ip];
            if (instruction == EBaitCodeList["movrr"]) EMOVRR(ref Registers, ref RAM);
            else if (instruction == EBaitCodeList["movrm"]) EMOVRM(ref Registers, ref RAM);
            else if (instruction == EBaitCodeList["movrc"]) EMOVRC(ref Registers, ref RAM);
            else if (instruction == EBaitCodeList["movmr"]) EMOVMR(ref Registers, ref RAM);
            else if (instruction == EBaitCodeList["movmc"]) EMOVMC(ref Registers, ref RAM);
        }
        public bool IsRealised(string instruction)
        {
            return instruction == "mov";
        }
        public void Init(Dictionary<string, byte> RegisterCodes_, Dictionary<string, int> RegisterSizes_, Dictionary<string, byte> BaitCodeList_, Dictionary<string, string> Flags_)
        {
            RegisterCodes = RegisterCodes_;
            RegisterSizes = RegisterSizes_;
            BaitCodeList = BaitCodeList_;
            Flags = Flags_;
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
            return baitCode == EBaitCodeList["movrr"] ||
                baitCode == EBaitCodeList["movrm"] ||
                baitCode == EBaitCodeList["movrc"] ||
                baitCode == EBaitCodeList["movmr"] ||
                baitCode == EBaitCodeList["movmc"];
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