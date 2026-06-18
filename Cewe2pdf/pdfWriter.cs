using iTextSharp.awt.geom;
using iTextSharp.text;
using iTextSharp.text.pdf;
using SkiaSharp;
using System;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace Cewe2pdf {

    class PdfWriter {

        private System.IO.FileStream _fileStream;
        private Document _doc = new Document();
        private iTextSharp.text.pdf.PdfWriter _writer;
        private Mcfx _mcfx;

        public PdfWriter(string pOutPath, Mcfx mcfx) {
            // TODO add more exception checking...
            _mcfx = mcfx;

            try {
                // Open file stream for exported pdf
                _fileStream = new System.IO.FileStream(pOutPath, System.IO.FileMode.Create);
            } catch (Exception e) {
                Log.Error("Creating pdf file at: '" + pOutPath + "' failed with error: '" + e.Message + "'.");
                return;
            }

            // initialize iTextSharp pdf writer
            _writer = iTextSharp.text.pdf.PdfWriter.GetInstance(_doc, _fileStream);

            // just put something in there, doesn't really matter...
            _doc.AddAuthor("Cewe2pdf.exe");
            _doc.AddCreator("Cewe2Pdf");
            _doc.AddTitle("ConvertedCewePhotobook");

            // TODO: move font loading to Config class?
            // necessary for loading .ttf it seams
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string fontPath = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? "C:\\Windows\\Fonts"
                : System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)
                    ? "/Library/Fonts" : "/usr/share/fonts";
            Log.Info("Loading fonts from " + fontPath);
            if (System.IO.Directory.Exists(fontPath)) FontFactory.RegisterDirectory(fontPath);

            string cwfontPath = System.IO.Path.Combine(Config.ProgramPath, "Resources", "photofun", "fonts");
            Log.Info("Loading fonts from " + cwfontPath);
            FontFactory.RegisterDirectory(cwfontPath);

            // recursivly search for fonts folders in full hps path
            string hpsPath = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? "C:\\ProgramData\\hps" : null;
            if (!string.IsNullOrEmpty(hpsPath) && System.IO.Directory.Exists(hpsPath)) {
                Log.Info("Searching for fonts directory at " + hpsPath);
                string[] fontDirs = System.IO.Directory.GetDirectories(hpsPath, "fonts", System.IO.SearchOption.AllDirectories);
                foreach (string dir in fontDirs) {
                    Log.Info("Loading fonts from: '" + dir + "'.");
                    FontFactory.RegisterDirectory(dir);
                }
            } else {
                Log.Warning("Directory at: '" + hpsPath + "' does not exist. Skipping.");
            }

            Log.Info("Found " + FontFactory.RegisteredFonts.Count + " fonts.");

            // start writing
            _doc.Open();
        }

        public void writePage(Page pPage) {

            // page size is given per <fotobook/page>. iTextSharp needs it set before adding page or opening document.
            _doc.SetPageSize(new Rectangle(0f, 0f, pPage.bundleSize.X, pPage.bundleSize.Y));

            // handle first page case
            try {
                if (!_doc.IsOpen()) _doc.Open();
                else _doc.NewPage();
            } catch (Exception e) {
                Log.Error("Creating pdf page failed with error: '" + e.Message + "'.");
                return;
            }


            PdfContentByte canvas = _writer.DirectContent;

            // TOOD: de-duplicate
            // draw left part of background
            if (pPage.backgroundLeft != null) {

                canvas.Rectangle(0, 0, pPage.bundleSize.X / 2, pPage.bundleSize.Y);
#if DEBUG || _DEBUG
                canvas.SetColorFill(BaseColor.CYAN);
#else
                canvas.SetColorFill(BaseColor.WHITE);
#endif
                canvas.Fill();

                string id = pPage.backgroundLeft;
                SKBitmap sysImg = DesignIdConverter.getImageFromID(id);
                if (sysImg == null) {
                    Log.Error("Background image for id '" + id + "' was null.");
#if DEBUG || _DEBUG
                    canvas.SetColorFill(BaseColor.MAGENTA);
#else
                    canvas.SetColorFill(BaseColor.WHITE);
#endif
                    canvas.Fill();
                } else {
                    Image img = sysImageToITextImage(sysImg);

                    float facY = pPage.bundleSize.Y / img.PlainHeight;
                    float facX = pPage.bundleSize.X / img.PlainWidth;
                    float fac = Math.Max(facX, facY);

                    img.ScalePercent(fac * 100f);

                    float yoffset = (img.ScaledHeight - pPage.bundleSize.Y) * -0.5f;
                    float xoffset = (img.ScaledWidth - pPage.bundleSize.X) * -0.5f;

                    img.SetAbsolutePosition(xoffset, yoffset);

                    float width = -xoffset + ((pPage.type == Page.Type.Fullcover) ? pPage.bundleSize.X : pPage.bundleSize.X / 2f);

                    Image imgCropped = cropImage(img, _writer, 0, 0, width, img.ScaledHeight);

                    imgCropped.SetAbsolutePosition(xoffset, yoffset);
                    canvas.AddImage(imgCropped);
                }
            }

            // draw right background
            if (pPage.backgroundRight != null) {

                canvas.Rectangle(pPage.bundleSize.X / 2, 0, pPage.bundleSize.X / 2, pPage.bundleSize.Y);
#if DEBUG || _DEBUG
                canvas.SetColorFill(BaseColor.CYAN);
                canvas.SetColorFill(BaseColor.WHITE);
#endif
                canvas.Fill();

                string id = pPage.backgroundRight;
                SKBitmap sysImg = DesignIdConverter.getImageFromID(id);
                if (sysImg == null) {
                    Log.Error("Background image for id '" + id + "' was null.");
#if DEBUG || _DEBUG
                    canvas.SetColorFill(BaseColor.MAGENTA);
#else
                canvas.SetColorFill(BaseColor.WHITE);
#endif
                    canvas.Fill();
                    canvas.Fill();
                } else {
                    Image img = sysImageToITextImage(sysImg);

                    float facY = pPage.bundleSize.Y / img.PlainHeight;
                    float facX = pPage.bundleSize.X / img.PlainWidth;
                    float fac = Math.Max(facX, facY);

                    img.ScalePercent(fac * 100f);

                    float yoffset = (img.ScaledHeight - pPage.bundleSize.Y) * -0.5f;
                    float xoffset = (img.ScaledWidth - pPage.bundleSize.X) * -0.5f;

                    img.SetAbsolutePosition(xoffset, yoffset);

                    Image imgCropped = cropImage(img, _writer, pPage.bundleSize.X / 2f, 0, img.ScaledWidth, img.ScaledHeight);

                    imgCropped.SetAbsolutePosition(xoffset + pPage.bundleSize.X / 2, yoffset);
                    canvas.AddImage(imgCropped);
                }
            }

            // draw all supported content areas stored in this page
            foreach (Area area in pPage.areas) {

                // calculate rect dimensions // TODO: de-duplicate?
                float pX = area.rect.X;
                float pY = pPage.bundleSize.Y - area.rect.Y - area.rect.Height;

                // handle rotation
                canvas.SaveState();
                AffineTransform tf = new AffineTransform();
                double angle = area.rotation * Math.PI / 180.0;
                tf.Rotate(-angle, pX + area.rect.Width / 2f, pY + area.rect.Height / 2f); // rotate around center ccw                                                                      
                canvas.ConcatCTM((float)tf.GetScaleX(), (float)tf.GetShearY(), (float)tf.GetShearX(), (float)tf.GetScaleY(), (float)tf.GetTranslateX(), (float)tf.GetTranslateY());

                if (area is ImageArea || area is ImageBackgroundArea) {
                    // TODO: This is somewhat hacky - there is probably a better way to do this.
                    if (area is ImageBackgroundArea) {
                        ImageBackgroundArea bgArea = (ImageBackgroundArea)area;

                        if (bgArea.type == ImageBackgroundArea.ImageBackgroundType.Right)
                            bgArea.rect.X += pPage.bundleSize.X / 2f + pPage.spineSize / 2f;
                    }

                    ImageArea imgArea = (ImageArea)area;

                    // if image path was not valid draw magenta outline and print error
                    if (imgArea.filename == "NULL") {
#if DEBUG || _DEBUG
                        // calculate rect dimensions
                        Rectangle nullRect = new Rectangle(pX, pY, pX + imgArea.rect.Width, pY + imgArea.rect.Height);

                        // configure border
                        nullRect.Border = 1 | 2 | 4 | 8;
                        nullRect.BorderColor = BaseColor.MAGENTA;
                        nullRect.BorderWidth = 4.0f;

                        // draw to document
                        canvas.Rectangle(nullRect);

                        Log.Error("Image path was null. Probably caused by an empty image area.");
                        canvas.RestoreState();
#endif
                        continue;
                    }

                    // load image file.
                    SKBitmap sysImg;
                    try {
                        sysImg = _mcfx.getSystemImageForFilename(imgArea.filename);
                    } catch (System.Exception e) {
                        Log.Error("Loading image failed with error: '" + e.Message + "'");
                        Log.Error("Loading image failed.");
                        canvas.RestoreState();
                        continue;
                    }

                    if (sysImg == null) {
                        Log.Error("Loading image failed.");
                        canvas.RestoreState();
                        continue;
                    }

                    // EXIF orientation already applied in Mcfx.getSystemImageForFilename

                    // calculate resizing factor, results in equal pixel density for all images.
                    float scale = 1f / imgArea.scale * Config.ImgScale; // the higher this value, the lower pixel density is. 0.0f = original resolution
                    scale = scale < 1.0f ? 1.0f : scale; // never scale image up

                    int newW = (int)(sysImg.Width / scale);
                    int newH = (int)(sysImg.Height / scale);

                    // resize image
                    sysImg = sysImg.Resize(new SKImageInfo(newW, newH), SKFilterQuality.Medium);

                    Image img = sysImageToITextImage(sysImg);

                    // apply scale as defined in .mcf
                    img.ScalePercent(imgArea.scale * 100.0f * scale);

                    // calculate image position in pdf page
                    float posX = imgArea.rect.X + imgArea.cutout.X;
                    float posY = pPage.bundleSize.Y - imgArea.rect.Y - imgArea.rect.Height; // pdf origin is in lower left, mcf origin is in upper left

                    // yaaaaa... whatever. This way everything fits
                    float cropBottom = img.ScaledHeight - imgArea.rect.Height + imgArea.cutout.Y;

                    // crop image to mcf specified rect
                    Image cropped = cropImage(img, _writer, -imgArea.cutout.X, cropBottom, imgArea.rect.Width, imgArea.rect.Height);

                    // move to mcf specified position
                    cropped.SetAbsolutePosition(imgArea.rect.X, posY);

                    string imgType = area is ImageBackgroundArea ? "ImageBackground" : "Image";
                    Log.Info("Rendering " + imgType + " (." + imgArea.filename.Split(".").Last() + "): " +
                        "scaled: " + newW + "x" + newH + "; " +
                        "cropped: " + (int)cropped.Width + "x" + (int)cropped.Height + "; " +
                        "at: " + (int)cropped.AbsoluteX + ", " + (int)cropped.AbsoluteY);

                    // draw the image
                    canvas.AddImage(cropped);

                    // draw image border if specified in .mcf
                    if (imgArea.border) {
                        // TODO mcf as an outside property that is currently not taken into account.
                        // seems like all borders are 'outside' in photobook.
                        // iTextSharp draws Borders centered (BorderWidth/2 pixels overlap image)
                        // this should be corrected.

                        // calc border rect
                        Rectangle rect = new Rectangle(pX, pY, pX + imgArea.rect.Width, pY + imgArea.rect.Height);

                        // configure border
                        rect.Border = 1 | 2 | 4 | 8;
                        rect.BorderColor = argb2BaseColor(imgArea.borderColor);
                        rect.BorderWidth = imgArea.borderWidth;

                        // draw border
                        canvas.Rectangle(rect);
                    }

                } else if (area is TextArea) {
                    TextArea textArea = (TextArea)area;

                    // Render text background if not transparent
                    if (!textArea.backgroundcolor.EndsWith("00")) {
                        Log.Info("Rendering Text background: color=" + textArea.backgroundcolor);

                        canvas.Rectangle(pX, pY, textArea.rect.Width, textArea.rect.Height);
                        canvas.SetColorFill(argb2BaseColor(textArea.backgroundcolor));
                        canvas.Fill();
                    }

                    // just in case something went wrong
                    if (String.IsNullOrWhiteSpace(textArea.text)) {
                        Log.Error("Text was empty.");
                        canvas.RestoreState();
                        continue;
                    } else {
                        Log.Info("Rendering Text: font=" + textArea.font + "; size=" + textArea.fontsize + "; align=" + textArea.align + "; valign=" + textArea.valign);
                    }

                    // iTextSharp textbox
                    ColumnText colText = new ColumnText(canvas);


                    // calculate rect
                    float llx = textArea.rect.X;
                    float lly = pPage.bundleSize.Y - textArea.rect.Y - textArea.rect.Height;
                    float urx = llx + textArea.rect.Width;
                    float ury = lly + textArea.rect.Height;
                    Rectangle textRect = new Rectangle(llx, lly, urx, ury);

                    // apply rect to textbox
                    colText.SetSimpleColumn(textRect);

                    // The actual text object
                    Paragraph par = new Paragraph();

                    // magic number that closely matches photobook
                    // TODO there is probably more information in the .mcf's css part
                    par.SetLeading(0, 1.3f);

                    // apply corrent alignment
                    if (textArea.align == "ALIGNHCENTER")
                        par.Alignment = Element.ALIGN_CENTER;
                    else if (textArea.align == "ALIGNLEFT")
                        par.Alignment = Element.ALIGN_LEFT;
                    else if (textArea.align == "ALIGNRIGHT")
                        par.Alignment = Element.ALIGN_RIGHT;
                    else if (textArea.align == "ALIGNJUSTIFY")
                        par.Alignment = Element.ALIGN_JUSTIFIED;
                    else
                        Log.Warning("Unhandled text align: '" + textArea.align + "'.");

                    // add text chunks
                    foreach (TextElement elem in textArea.textElements) {
                        int style = 0;
                        style += elem.bold ? Font.BOLD : 0;
                        style += elem.italic ? Font.ITALIC : 0;
                        style += elem.underlined ? Font.UNDERLINE : 0;
                        Font fnt = FontFactory.GetFont(elem.family, elem.size, style, argb2BaseColor(elem.color));

                        par.Add(new Chunk(elem.text + (elem.newline ? "\n" : " "), fnt));
                    }

                    int valign = 0;
                    if (textArea.valign == "ALIGNVCENTER")
                        valign = Element.ALIGN_MIDDLE;
                    else if (textArea.valign == "ALIGNVTOP")
                        valign = Element.ALIGN_TOP;
                    else if (textArea.valign == "ALIGNVBOTTOM")
                        valign = Element.ALIGN_BOTTOM;
                    else
                        Log.Warning("Unhandled text vertical align: '" + textArea.valign + "'.");

                    // v align needs a table...
                    PdfPTable table = new PdfPTable(1);
                    table.SetWidths(new int[] { 1 });
                    table.WidthPercentage = 100;
                    table.AddCell(new PdfPCell(par) {
                        HorizontalAlignment = par.Alignment,
                        VerticalAlignment = valign,
                        FixedHeight = textArea.rect.Height,
                        Border = 0,
                    });

                    // add paragraph to textbox
                    colText.AddElement(table);

                    // draw textbox
                    colText.Go();
                }

                // restore canvas transform before rotation
                canvas.RestoreState();
            }

            // draw pagenumbers
            // TODO remove magic numbers, at least comment
            const float PAGE_NR_Y_OFFSET = -4.0f;
            const float PAGE_NR_X_OFFSET = 0.0f;
            float PAGE_NR_FONT_SIZE = Page.pageNoFontSize * 1.1f;
            float PAGE_NR_HEIGHT = PAGE_NR_FONT_SIZE + 12.0f; // add some extra space... this is needed.
            float PAGE_Y_POS = Page.pageNoMargin.Y + PAGE_NR_Y_OFFSET;

            BaseColor pageNoColor = argb2BaseColor(Page.pageNoColor);

            // left
            Paragraph pageNoLeft = new Paragraph(pPage.pageNoLeft, FontFactory.GetFont(Page.pageNoFont, PAGE_NR_FONT_SIZE, pageNoColor));
            pageNoLeft.Alignment = Element.ALIGN_LEFT + Element.ALIGN_BOTTOM;

            ColumnText leftNo = new ColumnText(_writer.DirectContent);
            Rectangle leftNoRect = new Rectangle(Page.pageNoMargin.X + PAGE_NR_X_OFFSET, PAGE_Y_POS, 500, PAGE_Y_POS + PAGE_NR_HEIGHT);
            leftNo.SetSimpleColumn(leftNoRect);

            leftNo.AddElement(pageNoLeft);
            leftNo.Go();

            //leftNoRect.Border = 1 | 2 | 4 | 8;
            //leftNoRect.BorderColor = BaseColor.GREEN;
            //leftNoRect.BorderWidth = 1.0f;
            //_writer.DirectContent.Rectangle(leftNoRect);

            // right
            Paragraph pageNoRight = new Paragraph(pPage.pageNoRight, FontFactory.GetFont(Page.pageNoFont, PAGE_NR_FONT_SIZE, pageNoColor));
            pageNoRight.Alignment = Element.ALIGN_RIGHT;

            ColumnText rightNo = new ColumnText(_writer.DirectContent);
            Rectangle rightNoRect = new Rectangle(pPage.bundleSize.X - Page.pageNoMargin.X - PAGE_NR_X_OFFSET - 500, PAGE_Y_POS, pPage.bundleSize.X - Page.pageNoMargin.X - PAGE_NR_X_OFFSET, PAGE_Y_POS + PAGE_NR_HEIGHT);
            rightNo.SetSimpleColumn(rightNoRect);

            rightNo.AddElement(pageNoRight);
            rightNo.Go();

            //rightNoRect.Border = 1 | 2 | 4 | 8;
            //rightNoRect.BorderColor = BaseColor.YELLOW;
            //rightNoRect.BorderWidth = 1.0f;
            //_writer.DirectContent.Rectangle(rightNoRect);

            //Console.WriteLine("Page drawn: " + pPage.type.ToString() + " left: " + pPage.pageNoLeft + "; right: " + pPage.pageNoRight + "!");

        }

        public Image cropImage(Image image, iTextSharp.text.pdf.PdfWriter writer, float fromLeft, float fromBottom, float width, float height) {
            // from https://stackoverflow.com/a/14473667
            PdfContentByte cb = writer.DirectContent;
            PdfTemplate t = cb.CreateTemplate(width, height);
            float origWidth = image.ScaledWidth;
            float origHeight = image.ScaledHeight;
            t.AddImage(image, origWidth, 0, 0, origHeight, -fromLeft, -fromBottom);
            return Image.GetInstance(t);
        }

        public void close() {
            // close all files and filestreams...
            _doc.Close();
            _writer.Close();
            _fileStream.Close();
        }

        public static SKBitmap ExifRotate(SKBitmap img, SKEncodedOrigin origin) {
            // iText does not respect EXIF orientation — apply it manually via SkiaSharp
            bool swapDims = origin == SKEncodedOrigin.LeftTop || origin == SKEncodedOrigin.RightTop ||
                            origin == SKEncodedOrigin.LeftBottom || origin == SKEncodedOrigin.RightBottom;
            int w = swapDims ? img.Height : img.Width;
            int h = swapDims ? img.Width : img.Height;

            var rotated = new SKBitmap(w, h);
            using var canvas = new SKCanvas(rotated);
            canvas.Clear();

            switch (origin) {
                case SKEncodedOrigin.BottomRight:
                    canvas.RotateDegrees(180, w / 2f, h / 2f); break;
                case SKEncodedOrigin.RightTop:
                    canvas.Translate(w, 0); canvas.RotateDegrees(90); break;
                case SKEncodedOrigin.LeftBottom:
                    canvas.Translate(0, h); canvas.RotateDegrees(270); break;
                case SKEncodedOrigin.TopRight:
                    canvas.Scale(-1, 1, w / 2f, h / 2f); break;
                case SKEncodedOrigin.BottomLeft:
                    canvas.Scale(1, -1, w / 2f, h / 2f); break;
                default: break;
            }
            canvas.DrawBitmap(img, 0, 0);
            return rotated;
        }

        public static BaseColor argb2BaseColor(string color) {
            int argb = Int32.Parse(color.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return new BaseColor(r, g, b, a);
        }

        private static iTextSharp.text.Image sysImageToITextImage(SKBitmap pImg) {
            using var image = SKImage.FromBitmap(pImg);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
            return Image.GetInstance(data.ToArray());
        }
    }
}
