using GameAndDot.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using XProtocol;

namespace GameAndDot.Shared.Protocol
{
    public static class GameProtocol
    {
        public static byte[] SerializeMessage(EventMessege msg)
        {
            string json = JsonSerializer.Serialize(msg);
            byte[] data = Encoding.UTF8.GetBytes(json);

            var packet = XPacket.Create(1, 0);
            int offset = 0;
            byte fieldId = 0;

            while (offset < data.Length)
            {
                int chunkSize = Math.Min(255, data.Length - offset);
                var chunk = new byte[chunkSize];
                Array.Copy(data, offset, chunk, 0, chunkSize);

                packet.SetValueRaw(fieldId, chunk);

                offset += chunkSize;
                fieldId++;
            }

            return packet.ToPacket();
        }

        public static EventMessege? DeserializeMessage(byte[] packetBytes)
        {
            var packet = XPacket.Parse(packetBytes);
            if (packet == null)
                return null;

            if (packet.Fields == null || packet.Fields.Count == 0)
                return null;

            var orderedFields = packet.Fields
                .OrderBy(f => f.FieldID)
                .ToList();

            using var ms = new MemoryStream();

            foreach (var field in orderedFields)
            {
                if (field.Contents == null || field.FieldSize == 0)
                    continue;

                ms.Write(field.Contents, 0, field.Contents.Length);
            }

            var allBytes = ms.ToArray();
            string json = Encoding.UTF8.GetString(allBytes);

            return JsonSerializer.Deserialize<EventMessege>(json);
        }

        public static int FindPacketEndIndex(List<byte> data)
        {
            if (data.Count < 2)
                return -1;

            for (int i = 0; i < data.Count - 1; i++)
            {
                if (data[i] == 0xFF && data[i + 1] == 0x00)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
