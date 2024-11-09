using System;
using System.Collections.Generic;
using Xunit;
using Protos;
using Google.Protobuf;

namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTCommunicationLayer.Protobuf
{
    public class ProtobufTest
    {
        [Fact]
        public void SensorData_SerializationDeserialization_ShouldPreserveData()
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

        [Fact]
        public void IMUData_SerializationDeserialization_ShouldPreserveValues()
        {
            // Arrange
            var imuData = new IMUData
            {
                GyroX = 1.23f,
                GyroY = 4.56f,
                GyroZ = 7.89f,
                MagX = 0.12f,
                MagY = 0.34f,
                MagZ = 0.56f,
                AccX = 9.87f,
                AccY = 6.54f,
                AccZ = 3.21f
            };

            // Act
            var serializedData = imuData.ToByteArray();
            var deserializedData = IMUData.Parser.ParseFrom(serializedData);

            // Assert
            Assert.Equal(imuData.GyroX, deserializedData.GyroX);
            Assert.Equal(imuData.GyroY, deserializedData.GyroY);
            Assert.Equal(imuData.GyroZ, deserializedData.GyroZ);
            Assert.Equal(imuData.MagX, deserializedData.MagX);
            Assert.Equal(imuData.MagY, deserializedData.MagY);
            Assert.Equal(imuData.MagZ, deserializedData.MagZ);
            Assert.Equal(imuData.AccX, deserializedData.AccX);
            Assert.Equal(imuData.AccY, deserializedData.AccY);
            Assert.Equal(imuData.AccZ, deserializedData.AccZ);
        }

        [Fact]
        public void PPGData_SerializationDeserialization_ShouldPreserveValues()
        {
            // Arrange
            var ppgData = new PPGData
            {
                Led1 = 100,
                Led2 = 200,
                Led3 = 300,
                Temperature = 36.5f
            };

            // Act
            var serializedData = ppgData.ToByteArray();
            var deserializedData = PPGData.Parser.ParseFrom(serializedData);

            // Assert
            Assert.Equal(ppgData.Led1, deserializedData.Led1);
            Assert.Equal(ppgData.Led2, deserializedData.Led2);
            Assert.Equal(ppgData.Led3, deserializedData.Led3);
            Assert.Equal(ppgData.Temperature, deserializedData.Temperature);
        }

        [Fact]
        public void SensorData_WithEmptyDataMap_ShouldDeserializeCorrectly()
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

        [Fact]
        public void SensorData_WithMultipleEntriesInDataMap_ShouldStoreAndRetrieveCorrectly()
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
