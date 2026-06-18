using Microsoft.Data.Sqlite;
using System.Text;
using SkiaSharp;

namespace Cewe2pdf {

    class Mcfx {
        /* Quick Note:
         * .mcfx is 'just' a sqlite3 database file, containing 3 columns:
         *  | Data | Filename | LastModified |
         *  this class wraps SQL commands to access specific data blobs from the database
        */

        public Mcfx(string filePath) {
            _filePath = filePath;
        }

        public byte[] getDataForFilename(string filename) {
            // Provide the path to your existing database
            string connectionString = "Data Source=" + _filePath + ";";

            // Open the connection
            using (SqliteConnection conn = new SqliteConnection(connectionString)) {
                conn.Open();

                string selectQuery = "SELECT Data FROM Files WHERE Filename = @filename;";
                using (SqliteCommand cmd = new SqliteCommand(selectQuery, conn)) {
                    cmd.Parameters.AddWithValue("@filename", filename);
                    using (var reader = cmd.ExecuteReader()) {

                        if (reader.Read()) {
                            // Read the BLOB data from the database
                            byte[] blobData = reader["Data"] as byte[];

                            if (blobData != null) {
                                return blobData;
                            }
                        }
                    }
                }
            }

            Log.Error("No BLOB data found for filename " + filename);
            return null;
        }

        public SKBitmap getSystemImageForFilename(string filename) {
            byte[] blobData = getDataForFilename(filename);
            if (blobData == null) return null;

            SKBitmap bm = SKBitmap.Decode(blobData);
            if (bm == null) { Log.Error("SKBitmap.Decode returned null for '" + filename + "'."); return null; }

            // apply EXIF orientation from raw bytes
            using var codec = SKCodec.Create(new System.IO.MemoryStream(blobData));
            if (codec != null && codec.EncodedOrigin != SKEncodedOrigin.TopLeft && codec.EncodedOrigin != SKEncodedOrigin.Default)
                bm = PdfWriter.ExifRotate(bm, codec.EncodedOrigin);
            return bm;
        }

        public System.IO.MemoryStream getMcfFile() {
            byte[] blobData = getDataForFilename("data.mcf");
            if (blobData == null) return null;

            // TODO: first convert to string and cut off anything after </fotobook>
            // then convert to memory stream for returning
            // mcf has some binary data after </fotobook>, so we need to trim that off - not really sure what the data is for now
            string mcf = Encoding.UTF8.GetString(blobData).Split("</fotobook>")[0] + "</fotobook>";

            // mcf parser expects a memory stream, so convert mcf text back to binary
            byte[] trimmed = Encoding.UTF8.GetBytes(mcf);
            return new System.IO.MemoryStream(trimmed);
        }

        private string _filePath; // the database file path
    }
}
