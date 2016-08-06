using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MqMessage : IList<MqFrame> {

		public readonly List<MqFrame> Frames = new List<MqFrame>();

		public MqMessage() {

		}

		public byte[] ToByteArray() {
			var total_size = Frames.Sum(frame => frame.FrameLength);
			var buffer = new byte[total_size];
			using (var stream = new MemoryStream(buffer)) {

				for (var i = 0; i < Frames.Count; i++) {
					var frame = Frames[i];

					// Set the last frame in this message as the last.
					if (i == Frames.Count - 1) {
						frame.SetLast();
					}
					var bytes = frame.RawFrame();
					stream.Write(bytes, 0, bytes.Length);
				}

				return buffer;
			}
		}


		public IEnumerator<MqFrame> GetEnumerator() {
			return new List<MqFrame>(Frames).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new List<MqFrame>(Frames).GetEnumerator();
		}

		public void Add(MqFrame item) {
			Frames.Add(item);
		}

		public void Clear() {
			Frames.Clear();
		}

		public bool Contains(MqFrame item) {
			return Frames.Contains(item);
		}

		public void CopyTo(MqFrame[] array, int array_index) {
			Frames.CopyTo(array, array_index);
		}

		public bool Remove(MqFrame item) {
			return Frames.Remove(item);
		}

		public int Count => Frames.Count;
		public bool IsReadOnly => false;
		public int IndexOf(MqFrame item) {
			return Frames.IndexOf(item);
		}

		public void Insert(int index, MqFrame item) {
			Frames.Insert(index, item);
		}

		public void RemoveAt(int index) {
			Frames.RemoveAt(index);
		}

		public MqFrame this[int index] {
			get { return Frames[index]; }
			set { Frames[index] = value; }
		}
	}
}
