using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace DtronixMessageQueue.Tests.Mq {
	public class MqMessageWriterReaderTests {
		public ITestOutputHelper Output;
		private MqMessageWriter message_builder;
		private MqMessageReader message_reader;
		private MqSocketConfig config = new MqSocketConfig();

		private const string FillerText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

		public MqMessageWriterReaderTests(ITestOutputHelper output) {
			this.Output = output;
			message_builder = new MqMessageWriter(config);
			message_reader = new MqMessageReader();
		}

		[Fact]
		public void MessageWriter_writes_bool_true() {
			var expected_value = (bool) true;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadBoolean());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_bool_false() {
			var expected_value = (bool)false;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadBoolean());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_byte() {
			var expected_value = (byte)221;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadByte());
			Assert.True(message_reader.IsAtEnd);

		}

		[Fact]
		public void MessageWriter_writes_sbyte_positive() {
			var expected_value = (sbyte)101;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadSByte());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_sbyte_negative() {
			var expected_value = (sbyte)-101;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadSByte());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_short_positive() {
			var expected_value = (short)21457;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadInt16());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_short_negative() {
			var expected_value = (short)-21457;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadInt16());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_ushort() {
			var expected_value = (ushort)51574;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadUInt16());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_int_positive() {
			var expected_value = (int)515725234;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadInt32());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_int_negative() {
			var expected_value = (int)-515725234;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadInt32());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_uint() {
			var expected_value = (uint)1215725234;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadUInt32());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_long_positive() {
			var expected_value = (long)515352135236725234;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadInt64());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_long_negative() {
			var expected_value = (long)-515352135236725234;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadInt64());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_ulong() {
			var expected_value = (ulong)12231512365365725234;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadUInt64());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_float() {
			var expected_value = (float)123.456;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadSingle());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_double() {
			var expected_value = (double)12345.67891;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadDouble());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_decimal() {
			var expected_value = (decimal)9123456789123456789.9123456789123456789;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadDecimal());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_multi_frame_byte_array() {
			var expected_value = new byte[1024*32];
			var number = 0;
			for (int i = 0; i < 1024 * 32; i++) {
				if (number == 255) {
					number = 0;
				}

				expected_value[i] = (byte)number++;
			}
			message_builder.Write(expected_value, 0, expected_value.Length);
			var message = message_builder.ToMessage();
			VerifyMessageBytes(expected_value, message);

		}

		private void VerifyMessageBytes(byte[] expected_value, MqMessage message) {
			var byte_array_size = 0;

			foreach (var frame in message) {
				byte_array_size += frame.DataLength;
			}

			Assert.Equal(expected_value.Length, byte_array_size);

			var result_byte_array = new byte[byte_array_size];
			var position = 0;
			foreach (var frame in message) {
				Buffer.BlockCopy(frame.Buffer, 0, result_byte_array, position, frame.DataLength);
				position += frame.DataLength;
			}

			Assert.Equal(expected_value, result_byte_array);
		}

		[Fact]
		public void MessageWriter_writes_multi_frame_string_bytes() {
			var sb = new StringBuilder();

			for (int i = 0; i < 100; i++) {
				sb.Append(FillerText);
			}
			
			var expected_value = sb.ToString();
			var string_bytes = Encoding.UTF8.GetBytes(expected_value);
			var expected_bytes = new byte[string_bytes.Length + 4];
			var int_bytes = BitConverter.GetBytes(string_bytes.Length);
			Buffer.BlockCopy(int_bytes, 0, expected_bytes, 0, 4);

			Buffer.BlockCopy(string_bytes, 0, expected_bytes, 4, string_bytes.Length);

			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			VerifyMessageBytes(expected_bytes, message);
		}

		[Fact]
		public void MessageReader_reads_multi_frame_byte_array() {
			var expected_value = new byte[1024 * 32];
			var number = 0;
			for (int i = 0; i < 1024 * 32; i++) {
				if (number == 255) {
					number = 0;
				}

				expected_value[i] = (byte)number++;
			}
			message_builder.Write(expected_value, 0, expected_value.Length);
			var message = message_builder.ToMessage();
			var actual_value = new byte[expected_value.Length];
			message_reader.Message = message;

			var read = message_reader.Read(actual_value, 0, actual_value.Length);

			Assert.Equal(expected_value.Length, read);
			Assert.Equal(expected_value, actual_value);
			Assert.True(message_reader.IsAtEnd);

		}


		[Fact]
		public void MessageWriter_writes_string() {
			var expected_value = FillerText;
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadString());
			Assert.True(message_reader.IsAtEnd);
		}



		[Fact]
		public void MessageReader_reads_multi_frame_string() {
			var sb = new StringBuilder();

			for (int i = 0; i < 100; i++) {
				sb.Append(FillerText);
			}

			var expected_value = sb.ToString();
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadString());
			Assert.True(message_reader.IsAtEnd);

		}


		[Fact]
		public void MessageWriter_multiple_reads_writes() {
			message_builder.Write(true);
			message_builder.Write(false);

			message_builder.Write((char)'D');
			message_builder.Write(new char[] { 'A', 'Y', 'X', '0', '9', '8'});

			message_builder.Write((byte)214);
			message_builder.Write((sbyte)125);
			message_builder.Write((sbyte)-125);

			message_builder.Write((short)4513);
			message_builder.Write((short)-4513);
			message_builder.Write((ushort)43513);

			message_builder.Write((int)236236231);
			message_builder.Write((int)-236236231);
			message_builder.Write((uint)2362326231);

			message_builder.Write((long)2362362312561531);
			message_builder.Write((long)-2362362312561531);
			message_builder.Write((ulong)2362362312561531125);

			message_builder.Write((float)1234.56789);
			message_builder.Write((double)123467.5678912);
			message_builder.Write((decimal)123456789123456789.123456789123456789);

			var expected_byte_array = Utilities.SequentialBytes(50);
			message_builder.Write(expected_byte_array, 0, expected_byte_array.Length);

			message_builder.Write(FillerText);

			var message = message_builder.ToMessage();

			message_reader.Message = message;

			Assert.Equal(true, message_reader.ReadBoolean());
			Assert.Equal(false, message_reader.ReadBoolean());

			Assert.Equal('D', message_reader.ReadChar());
			Assert.Equal(new char[] { 'A', 'Y', 'X', '0', '9', '8' }, message_reader.ReadChars(6));


			Assert.Equal((byte)214, message_reader.ReadByte());
			Assert.Equal((sbyte)125, message_reader.ReadSByte());
			Assert.Equal((sbyte)-125, message_reader.ReadSByte());

			Assert.Equal((short)4513, message_reader.ReadInt16());
			Assert.Equal((short)-4513, message_reader.ReadInt16());
			Assert.Equal((ushort)43513, message_reader.ReadUInt16());

			Assert.Equal((int)236236231, message_reader.ReadInt32());
			Assert.Equal((int)-236236231, message_reader.ReadInt32());
			Assert.Equal((uint)2362326231, message_reader.ReadUInt32());

			Assert.Equal((long)2362362312561531, message_reader.ReadInt64());
			Assert.Equal((long)-2362362312561531, message_reader.ReadInt64());
			Assert.Equal((ulong)2362362312561531125, message_reader.ReadUInt64());

			Assert.Equal((float)1234.56789, message_reader.ReadSingle());
			Assert.Equal((double)123467.5678912, message_reader.ReadDouble());
			Assert.Equal((decimal)123456789123456789.123456789123456789, message_reader.ReadDecimal());

			var read_byte_array = new byte[50];
			message_reader.Read(read_byte_array, 0, read_byte_array.Length);
			Assert.Equal(expected_byte_array, read_byte_array);

			Assert.Equal(FillerText, message_reader.ReadString());
			Assert.True(message_reader.IsAtEnd);
		}


		[Fact]
		public void MessageWriter_writes_char() {
			var expected_value = (char)'D';
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadChar());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_char_array() {
			var expected_value = new char[] {'A', 'B', 'C', '1', '2', '3'};
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadChars(expected_value.Length));
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageWriter_writes_char_array_slice() {
			var input_value = new char[] { 'A', 'B', 'C', '1', '2', '3' };
			var expected_value = new char[] { 'B', 'C', '1', '2'};
			message_builder.Write(input_value, 1, 4);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadChars(expected_value.Length));
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageReader_peeks_char() {
			var expected_value = new char[] { 'D', 'Z' };
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value[0], message_reader.PeekChar());
			Assert.Equal(expected_value, message_reader.ReadChars(expected_value.Length));
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageReader_reads_to_end() {
			var expected_value = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
			message_builder.Write(expected_value);
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadToEnd());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageReader_reads_to_end_multi_frame() {
			var expected_value = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0};

			message_builder.Write(new byte[] { 1, 2, 3, 4, 5 });
			message_builder.FinalizeFrame();
			
			message_builder.Write(new byte[] { 6, 7, 8, 9, 0 });
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Equal(expected_value, message_reader.ReadToEnd());
			Assert.True(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageReader_reads_to_end_partial() {
			var expected_value = new byte[] { 4, 5, 6, 7, 8, 9, 10 };
			message_builder.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			message_reader.ReadBytes(3);
			Assert.Equal(expected_value, message_reader.ReadToEnd());
			Assert.True(message_reader.IsAtEnd);
		}


		[Fact]
		public void MessageReader_maintains_position() {
			var expected_value = 5;
			message_builder.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
			var message = message_builder.ToMessage();
			message_reader.Message = message;

			message_reader.ReadBytes(5);
			Assert.Equal(expected_value, message_reader.Position);
			Assert.False(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageReader_maintains_position_across_frames() {
			var expected_value = 7;
			message_builder.Write(new byte[] {1, 2, 3, 4});
			message_builder.FinalizeFrame();
			message_builder.Write(new byte[] {5, 6, 7, 8, 9, 10});

			var message = message_builder.ToMessage();
			message_reader.Message = message;


			Assert.Equal(new byte[] {1, 2, 3, 4, 5, 6, 7}, message_reader.ReadBytes(7));
			Assert.Equal(expected_value, message_reader.Position);
			Assert.False(message_reader.IsAtEnd);
		}

		[Fact]
		public void MessageReader_throws_when_reading_simple_type_across_frames() {
			message_builder.Write(new byte[] { 1, 2 });
			message_builder.FinalizeFrame();
			message_builder.Write(new byte[] { 5, 6 });

			var message = message_builder.ToMessage();
			message_reader.Message = message;

			Assert.Throws<InvalidOperationException>(() => message_reader.ReadInt32());
		}


	}
}