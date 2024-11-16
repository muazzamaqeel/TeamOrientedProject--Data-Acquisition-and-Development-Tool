using System;
using Xunit;
using Protos;
using Google.Protobuf;

namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTCommunicationLayer.Protobuf
{
    public class ProtobufTest
    {
        [Fact]
        public void Protobuf_SerializeDeserialize()
        {
            // Arrange
            var sensorData = new SensorData
            {
                PacifierId = "Pacifier_1",
                SensorType = "IMU"
            };
            sensorData.DataMap.Add("gyro_x", ByteString.CopyFrom(BitConverter.GetBytes(1.23f)));
            sensorData.DataMap.Add("gyro_y", ByteString.CopyFrom(BitConverter.GetBytes(4.56f)));

            // Act
            var serializedData = sensorData.ToByteArray();
            var deserializedData = SensorData.Parser.ParseFrom(serializedData);

            // Assert
            Assert.Equal(sensorData.PacifierId, deserializedData.PacifierId);
            Assert.Equal(sensorData.SensorType, deserializedData.SensorType);
            Assert.Equal(sensorData.DataMap.Count, deserializedData.DataMap.Count);
            Assert.True(deserializedData.DataMap.ContainsKey("gyro_x"));
            Assert.True(deserializedData.DataMap.ContainsKey("gyro_y"));
        }
        /// <summary>
        /// This test case is to check if the DataMap is empty when no data is added to it.
        /// </summary>
        [Fact]
        public void Protobuf_DataMap()
        {
            // Arrange
            var sensorData = new SensorData
            {
                PacifierId = "Pacifier_2",
                SensorType = "PPG",
                DataMap = { }
            };

            // Act
            var serializedData = sensorData.ToByteArray();
            var deserializedData = SensorData.Parser.ParseFrom(serializedData);

            // Assert
            Assert.Equal(sensorData.PacifierId, deserializedData.PacifierId);
            Assert.Equal(sensorData.SensorType, deserializedData.SensorType);
            Assert.Empty(deserializedData.DataMap);
        }

        /// <summary>
        /// This test case is to check if the DataMap is populated with multiple data entries.
        /// </summary>
        [Fact]
        public void Protobuf_DataMapIntense()
        {
            // Arrange
            var sensorData = new SensorData
            {
                PacifierId = "Pacifier_3",
                SensorType = "IMU"
            };
            sensorData.DataMap.Add("acc_x", ByteString.CopyFrom(BitConverter.GetBytes(2.34f)));
            sensorData.DataMap.Add("acc_y", ByteString.CopyFrom(BitConverter.GetBytes(3.45f)));
            sensorData.DataMap.Add("acc_z", ByteString.CopyFrom(BitConverter.GetBytes(4.56f)));

            // Act
            var serializedData = sensorData.ToByteArray();
            var deserializedData = SensorData.Parser.ParseFrom(serializedData);

            // Assert
            Assert.Equal(sensorData.DataMap.Count, deserializedData.DataMap.Count);
            Assert.True(deserializedData.DataMap.ContainsKey("acc_x"));
            Assert.True(deserializedData.DataMap.ContainsKey("acc_y"));
            Assert.True(deserializedData.DataMap.ContainsKey("acc_z"));
        }
    }
}
