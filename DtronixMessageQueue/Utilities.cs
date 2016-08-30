﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {

	/// <summary>
	/// Static helper utility class.
	/// </summary>
	public static class Utilities {

		/// <summary>
		/// Creates a frame with the specified parameters.
		/// Fixes common issues with frame creation.
		/// </summary>
		/// <param name="bytes">Byte buffer to add to this frame.</param>
		/// <param name="type">Type of frame to create.</param>
		/// <param name="config">Socket configurations for the frame to use.</param>
		/// <returns>Configured frame.</returns>
		public static MqFrame CreateFrame(byte[] bytes, MqFrameType type, MqSocketConfig config) {
			if (type == MqFrameType.Ping || type == MqFrameType.Empty || type == MqFrameType.EmptyLast) {
				bytes = null;
			}
			return new MqFrame(bytes, type, config);
		}
	}
}
