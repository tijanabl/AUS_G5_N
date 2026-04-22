using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusWriteCommandParameters p = CommandParameters as ModbusWriteCommandParameters;
            byte[] request = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Length)), 0, request, 4, 2);
            request[6] = p.UnitId;
            request[7] = p.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.OutputAddress)), 0, request, 8, 2);
            // Coil ON = 0xFF00, OFF = 0x0000 (Modbus standard)
            request[10] = (byte)(p.Value == 1 ? 0xFF : 0x00);
            request[11] = 0x00;
            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusWriteCommandParameters p = CommandParameters as ModbusWriteCommandParameters;
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();
            if (response[7] == p.FunctionCode + 0x80)
                HandeException(response[8]);
            ushort address = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(response, 8));
            ushort rawValue = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(response, 10));
            ushort value = (ushort)(rawValue == 0xFF00 ? 1 : 0);
            result.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address), value);
            return result;
        }
    }
}