using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AVC;

namespace AVC
{
    class Commands
    {
        public static bool IsConstant(string code, Dictionary<string, byte> RegisterCodes)
        {
            return !(IsRegister(code, RegisterCodes) ^ IsMemory(code, RegisterCodes));
        }
        public static bool IsMemory(string code, Dictionary<string, byte> RegisterCodes)
        {
            if (code.Length <= 2)
                return false;
            return IsRegister(code.Substring(1, code.Length - 2), RegisterCodes);
        }
        public static bool IsRegister(string code, Dictionary<string, byte> RegisterCodes)
        {
            byte rubbish;
            return RegisterCodes.TryGetValue(code, out rubbish);
        }
        public static byte[] CompleteToLenght(byte[] line, int targetLenght)
        {
            List<byte> result = new List<byte>(line);
            if (targetLenght > result.Count)
            {
                for (int i = result.Count; i < targetLenght; i++)
                {
                    byte tmp = 0;
                    result.Add(tmp);
                }
            }
            else
            {
                for (int i = result.Count; i > targetLenght; i--)
                    result.RemoveAt(result.Count - 1);

            }
            return result.ToArray();
        }
        public static byte[] ConvertToByte(long value, int size)
        {
            if (size == 1)
            {
                List<byte> tmp = new List<byte>();
                tmp.Add(Convert.ToByte(value));
                return tmp.ToArray();
            }
            if (size == 2)
                return BitConverter.GetBytes((short)value);
            if (size == 4)
                return BitConverter.GetBytes((int)value);
            if (size == 8)
                return BitConverter.GetBytes((long)value);
            return null;
        }
        public static string[] GetArguments(string instruction)
        {
            List<string> result = new List<string>();
            instruction = instruction.Substring(instruction.IndexOf(' ') + 1);
            int indexcomma;
            string nextcommand = "";
            while (true)
            {
                indexcomma = instruction.IndexOf(',');
                if (indexcomma == -1)
                    break;
                nextcommand = instruction.Substring(0, indexcomma);
                ClearCommand(ref nextcommand);
                result.Add(nextcommand);
                instruction = instruction.Substring(indexcomma + 1);
            }
            ClearCommand(ref instruction);
            result.Add(instruction);
            return result.ToArray();
        }
        public static void ClearCommand(ref string Command, char rubbish = ' ')
        {
            while (true)
            {
                int indexrub = Command.IndexOf(rubbish);
                if (indexrub == -1)
                    return;
                Command = Command.Remove(indexrub, 1);
            }

        }
        
    }
}
namespace AVM
{
    public class RM
    {
        private static Dictionary<int, int> ERegSize;
        private static Dictionary<string, int> ERegCode;
        public static  void Init(Dictionary<string, int> ERegCode_, Dictionary<int, int> ERegSize_)
        {
            ERegCode = ERegCode_;
            ERegSize = ERegSize_;
        }
        public static int ReadReg(string reg, byte[] Registers)
        {
            List<byte> byteip = new List<byte>();
            for (int i = 0; i < ERegSize[ERegCode[reg]]; i++)
                byteip.Add(Registers[ERegCode[reg] + i]);
            if (byteip.Count == 1)
                return Convert.ToInt32(byteip[0]);
            return BitConverter.ToInt32(Commands.CompleteToLenght(byteip.ToArray(), 4), 0);
        }
        public static int ReadReg(int addr, byte[] Registers)
        {
            List<byte> byteip = new List<byte>();
            for (int i = 0; i < ERegSize[addr]; i++)
                byteip.Add(Registers[addr + i]);
            return BitConverter.ToInt32(Commands.CompleteToLenght(byteip.ToArray(), 4), 0);
        }
        public static void WriteReg(int val, string reg, ref byte[] Registers)
        {
            byte[] newip = BitConverter.GetBytes(val);
            for (int i = 0; i < ERegSize[ERegCode[reg]]; i++)
                Registers[ERegCode[reg] + i] = newip[i];
        }
        public static void WriteReg(int val, int addr, ref byte[] Registers)
        {
            byte[] newip = BitConverter.GetBytes(val);
            for (int i = 0; i < ERegSize[addr]; i++)
                Registers[addr + i] = newip[i];
        }
        public static int ReadMem(int addr, int bytecount, ref byte[] RAM)
        {
            List<byte> result = new List<byte>();
            for (int i = 0; i < bytecount; i++)
                result.Add(RAM[addr + i]);
            return BitConverter.ToInt32(Commands.CompleteToLenght(result.ToArray(), 4), 0);
        }
    }
    class VMCommands
    {
        public static bool IsConstant(string code, Dictionary<string, byte> RegisterCodes)
        {
            return !(IsRegister(code, RegisterCodes) ^ IsMemory(code, RegisterCodes));
        }
        public static bool IsMemory(string code, Dictionary<string, byte> RegisterCodes)
        {
            if (code.Length <= 2)
                return false;
            return IsRegister(code.Substring(1, code.Length - 2), RegisterCodes);
        }
        public static bool IsRegister(string code, Dictionary<string, byte> RegisterCodes)
        {
            byte rubbish;
            return RegisterCodes.TryGetValue(code, out rubbish);
        }
        public static byte[] CompleteToLenght(byte[] line, int targetLenght)
        {
            List<byte> result = new List<byte>(line);
            if (targetLenght > result.Count)
            {
                for (int i = result.Count; i < targetLenght; i++)
                {
                    byte tmp = 0;
                    result.Add(tmp);
                }
            }
            else
            {
                for (int i = result.Count; i > targetLenght; i--)
                    result.RemoveAt(result.Count - 1);

            }
            return result.ToArray();
        }
        public static byte[] ConvertToByte(long value, int size)
        {
            if (size == 1)
            {
                List<byte> tmp = new List<byte>();
                tmp.Add(Convert.ToByte(value));
                return tmp.ToArray();
            }
            if (size == 2)
                return BitConverter.GetBytes((short)value);
            if (size == 4)
                return BitConverter.GetBytes((int)value);
            if (size == 8)
                return BitConverter.GetBytes((long)value);
            return null;
        }
        public static string[] GetArguments(string instruction)
        {
            List<string> result = new List<string>();
            instruction = instruction.Substring(instruction.IndexOf(' ') + 1);
            int indexcomma;
            string nextcommand = "";
            while (true)
            {
                indexcomma = instruction.IndexOf(',');
                if (indexcomma == -1)
                    break;
                nextcommand = instruction.Substring(0, indexcomma);
                ClearCommand(ref nextcommand);
                result.Add(nextcommand);
                instruction = instruction.Substring(indexcomma + 1);
            }
            ClearCommand(ref instruction);
            result.Add(instruction);
            return result.ToArray();
        }
        public static void ClearCommand(ref string Command, char rubbish = ' ')
        {
            while (true)
            {
                int indexrub = Command.IndexOf(rubbish);
                if (indexrub == -1)
                    return;
                Command = Command.Remove(indexrub, 1);
            }

        }

    }
}