// clsWebP, by Jose M. Piñeiro
// Website: https://github.com/JosePineiro/WebP-wapper
// Version: 1.0.0.9 (May 23, 2020)

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WebPWrapper;

#pragma warning disable IDE0007 // Use implicit type

namespace WebPTest
{
	public partial class WebPExample : Form
	{
		#region | Constructors |
		public WebPExample()
		{
			this.InitializeComponent();
		}

		private void WebPExample_Load(object sender, EventArgs e)
		{
			try {
				//Inform of execution mode
				this.Text = Application.ProductName + (IntPtr.Size == 8 ? " x64 v" : " x86 v") + Application.ProductVersion;

				//Inform of libWebP version
				this.Text += " (libwebp v" + WebP.GetVersion() + ")";
			} catch (Exception ex) {
				MessageBox.Show(ex.Message + "\r\nIn WebPExample.WebPExample_Load", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		#endregion

		#region << Events >>
		/// <summary>
		/// Test for load from file function
		/// </summary>
		private void ButtonLoad_Click(object sender, EventArgs e)
		{
			try {
				using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
					openFileDialog.Filter = "Image files (*.webp, *.png, *.tif, *.tiff)|*.webp;*.png;*.tif;*.tiff";
					openFileDialog.FileName = "";
					if (openFileDialog.ShowDialog() == DialogResult.OK) {
						buttonSave.Enabled = true;
						buttonSave.Enabled = true;
						string pathFileName = openFileDialog.FileName;

						if (Path.GetExtension(pathFileName) == ".webp") {
							pictureBox.Image = WebP.Load(pathFileName);
						} else {
							pictureBox.Image = Image.FromFile(pathFileName);
						}
					}
				}
			} catch (Exception ex) {
				MessageBox.Show(ex.Message + "\r\nIn WebPExample.buttonLoad_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Test for load thumbnail function
		/// </summary>
		private void ButtonThumbnail_Click(object sender, EventArgs e)
		{
			try {
				using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
					openFileDialog.Filter = "WebP files (*.webp)|*.webp";
					openFileDialog.FileName = "";
					if (openFileDialog.ShowDialog() == DialogResult.OK) {
						string pathFileName = openFileDialog.FileName;

						byte[] rawWebP = File.ReadAllBytes(pathFileName);
						pictureBox.Image = WebP.GetThumbnailQuality(rawWebP, 200, 150);
					}
				}
			} catch (Exception ex) {
				MessageBox.Show(ex.Message + "\r\nIn WebPExample.buttonThumbnail_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Test for advanced decode function
		/// </summary>
		private void ButtonCropFlip_Click(object sender, EventArgs e)
		{
			try {
				using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
					openFileDialog.Filter = "WebP files (*.webp)|*.webp";
					openFileDialog.FileName = "";
					if (openFileDialog.ShowDialog() == DialogResult.OK) {
						string pathFileName = openFileDialog.FileName;

						byte[] rawWebP = File.ReadAllBytes(pathFileName);
						WebPDecoderOptions decoderOptions = new WebPDecoderOptions
						{
							use_cropping = 1,
							crop_top = 10,          //Top beginning of crop area
							crop_left = 10,         //Left beginning of crop area
							crop_height = 250,       //Height of crop area
							crop_width = 300,        //Width of crop area
							use_threads = 1,         //Use multi-threading
							flip = 1                //Flip the image
						};
						pictureBox.Image = WebP.Decode(rawWebP, decoderOptions);
					}
				}
			} catch (Exception ex) {
				MessageBox.Show(ex.Message + "\r\nIn WebPExample.buttonCrop_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Test encode functions
		/// </summary>
		private void ButtonSave_Click(object sender, EventArgs e)
		{
			Control.CheckForIllegalCrossThreadCalls = false;
			byte[] rawWebP;

			try {
				if (pictureBox.Image == null) {
					MessageBox.Show("Please, load an image first");
				}

				//get the picture box image
				Bitmap bmp = (Bitmap)pictureBox.Image;

				//Test simple encode in lossy mode in memory with quality 75
				string lossyFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleLossy.webp");
				rawWebP = WebP.EncodeLossy(bmp, 75);
				File.WriteAllBytes(lossyFileName, rawWebP);
				MessageBox.Show("Made " + lossyFileName, "Simple lossy");

				//Test simple encode in lossless mode in memory
				string simpleLosslessFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleLossless.webp");
				rawWebP = WebP.EncodeLossless(bmp);
				File.WriteAllBytes(simpleLosslessFileName, rawWebP);
				MessageBox.Show("Made " + simpleLosslessFileName, "Simple lossless");

				//Test encode in lossy mode in memory with quality 75 and speed 9
				string advanceLossyFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AdvanceLossy.webp");
				WebPAuxStats lossyStats;
				rawWebP = WebP.EncodeLossy(bmp, 71, 9, true, out lossyStats);
				ShowCompressionStatistics(lossyStats, bmp);
				File.WriteAllBytes(advanceLossyFileName, rawWebP);
				MessageBox.Show("Made " + advanceLossyFileName, "Advance lossy");

				//Test advance encode lossless mode in memory with speed 9
				string losslessFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AdvanceLossless.webp");
				rawWebP = WebP.EncodeLossless(bmp, 9);
				File.WriteAllBytes(losslessFileName, rawWebP);
				MessageBox.Show("Made " + losslessFileName, "Advance lossless");

				//Test encode near lossless mode in memory with quality 40 and speed 9
				// quality 100: No-loss (bit-stream same as -lossless).
				// quality 80: Very very high PSNR (around 54dB) and gets an additional 5-10% size reduction over WebP-lossless image.
				// quality 60: Very high PSNR (around 48dB) and gets an additional 20%-25% size reduction over WebP-lossless image.
				// quality 40: High PSNR (around 42dB) and gets an additional 30-35% size reduction over WebP-lossless image.
				// quality 20 (and below): Moderate PSNR (around 36dB) and gets an additional 40-50% size reduction over WebP-lossless image.
				string nearLosslessFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NearLossless.webp");
				rawWebP = WebP.EncodeNearLossless(bmp, 40, 9);
				File.WriteAllBytes(nearLosslessFileName, rawWebP);
				MessageBox.Show("Made " + nearLosslessFileName, "Near lossless");

				MessageBox.Show("End of Test");
			} catch (Exception ex) {
				MessageBox.Show(ex.Message + "\r\nIn WebPExample.buttonSave_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Test GetPictureDistortion function
		/// </summary>
		private void ButtonMeasure_Click(object sender, EventArgs e)
		{
			try {
				if (pictureBox.Image == null) {
					MessageBox.Show("Please, load an reference image first");
				}

				using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
					openFileDialog.Filter = "WebP images (*.webp)|*.webp";
					openFileDialog.FileName = "";
					if (openFileDialog.ShowDialog() == DialogResult.OK) {
						Bitmap source;
						Bitmap reference;
						float[] result;

						//Load Bitmaps
						source = (Bitmap)pictureBox.Image;
						reference = WebP.Load(openFileDialog.FileName);

						//Measure PSNR
						result = WebP.GetPictureDistortion(source, reference, 0);
						MessageBox.Show("Red: " + result[0] + "dB.\nGreen: " + result[1] + "dB.\nBlue: " + result[2] + "dB.\nAlpha: " + result[3] + "dB.\nAll: " + result[4] + "dB.", "PSNR");

						//Measure SSIM
						result = WebP.GetPictureDistortion(source, reference, 1);
						MessageBox.Show("Red: " + result[0] + "dB.\nGreen: " + result[1] + "dB.\nBlue: " + result[2] + "dB.\nAlpha: " + result[3] + "dB.\nAll: " + result[4] + "dB.", "SSIM");

						//Measure LSIM
						result = WebP.GetPictureDistortion(source, reference, 2);
						MessageBox.Show("Red: " + result[0] + "dB.\nGreen: " + result[1] + "dB.\nBlue: " + result[2] + "dB.\nAlpha: " + result[3] + "dB.\nAll: " + result[4] + "dB.", "LSIM");
					}
				}
			} catch (Exception ex) {
				MessageBox.Show(ex.Message + "\r\nIn WebPExample.buttonMeasure_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Test GetInfo function
		/// </summary>
		private void ButtonInfo_Click(object sender, EventArgs e)
		{
			try {
				using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
					openFileDialog.Filter = "WebP images (*.webp)|*.webp";
					openFileDialog.FileName = "";
					if (openFileDialog.ShowDialog() == DialogResult.OK) {
						string pathFileName = openFileDialog.FileName;

						byte[] rawWebp = File.ReadAllBytes(pathFileName);
						WebPInfo info = WebP.GetInfo(rawWebp);
						MessageBox.Show("Width: " + info.Width + "\n" +
										"Height: " + info.Height + "\n" +
										"Has alpha: " + info.HasAlpha + "\n" +
										"Is animation: " + info.IsAnimated + "\n" +
										"Format: " + info.Format, "Information");
					}
				}
			} catch (Exception ex) {
				MessageBox.Show(ex.Message + "\r\nIn WebPExample.buttonInfo_Click", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		#endregion

		private static void ShowCompressionStatistics(WebPAuxStats stats, Bitmap gdiPlusImage)
		{
			MessageBox.Show("Dimensions: " + gdiPlusImage.Width + " x " + gdiPlusImage.Height + " pixels\n" +
				"Output:    " + stats.coded_size + " bytes\n" +
				"PSNR Y:    " + stats.PSNRY + " db\n" +
				"PSNR u:    " + stats.PSNRU + " db\n" +
				"PSNR v:    " + stats.PSNRV + " db\n" +
				"PSNR ALL:  " + stats.PSNRALL + " db\n" +
				"Block intra4:  " + stats.block_count_intra4 + "\n" +
				"Block intra16: " + stats.block_count_intra16 + "\n" +
				"Block skipped: " + stats.block_count_skipped + "\n" +
				"Header size:    " + stats.header_bytes + " bytes\n" +
				"Mode-partition: " + stats.mode_partition_0 + " bytes\n" +
				"Macro-blocks 0: " + stats.segment_size_segments0 + " residuals bytes\n" +
				"Macro-blocks 1: " + stats.segment_size_segments1 + " residuals bytes\n" +
				"Macro-blocks 2: " + stats.segment_size_segments2 + " residuals bytes\n" +
				"Macro-blocks 3: " + stats.segment_size_segments3 + " residuals bytes\n" +
				"Quantizer    0: " + stats.segment_quant_segments0 + " residuals bytes\n" +
				"Quantizer    1: " + stats.segment_quant_segments1 + " residuals bytes\n" +
				"Quantizer    2: " + stats.segment_quant_segments2 + " residuals bytes\n" +
				"Quantizer    3: " + stats.segment_quant_segments3 + " residuals bytes\n" +
				"Filter level 0: " + stats.segment_level_segments0 + " residuals bytes\n" +
				"Filter level 1: " + stats.segment_level_segments1 + " residuals bytes\n" +
				"Filter level 2: " + stats.segment_level_segments2 + " residuals bytes\n" +
				"Filter level 3: " + stats.segment_level_segments3 + " residuals bytes\n", "Compression statistics");
		}
	}
}