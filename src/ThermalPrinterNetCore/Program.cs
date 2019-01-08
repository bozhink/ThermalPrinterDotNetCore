namespace ThermalPrinterNetCore
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class Program
    {
        public static void Main()
        {
            using (var stream = new FileStream("/dev/usb/lp1", FileMode.Open, FileAccess.Write))
            {
                var printer = new ThermalPrinter(stream);

                printer.WakeUp();

                //TestReceipt(printer);

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                printer.WriteLine_Bold("BG - Всичкo");
                printer.LineFeed();

                // printer.SetAlignCenter();
                // printer.WriteLine("MY SHOP", (byte)ThermalPrinter.PrintingStyle.DoubleHeight + (byte)ThermalPrinter.PrintingStyle.DoubleWidth);
                // printer.WriteLine("My address, CITY");
                // printer.LineFeed();
                // printer.LineFeed();

                // printer.SetAlignLeft();

                // printer.HorizontalLine(32);

                // printer.BoldOn();
                // printer.WriteLine("Written line");
                // printer.BoldOff();

                // printer.ItalicOn();
                // printer.WriteLine("Written line");
                // printer.ItalicOff();

                // printer.BoldOn();
                // printer.ItalicOn();
                // printer.WriteLine("Written line");
                // printer.ItalicOff();
                // printer.BoldOff();

                // printer.WriteLine("Written line");

                // printer.LineFeed();
                printer.PrintCodeTable();
                printer.LineFeed();

                // printer.SetAlignCenter();
                // printer.PrintQRCode("QRCODE TEST123456789");
                // printer.SetAlignLeft();
                // printer.LineFeed();

                // printer.WriteLine("Written line");

                // TestBarcode(printer);

                // printer.LineFeed();
                // printer.HorizontalLine(32);

                // printer.LineFeed();
                // printer.LineFeed();
                printer.LineFeed();
                printer.PaperCut();

                printer.Sleep();

                stream.Flush();
                stream.Close();
            }
        }

        private static void TestReceipt(ThermalPrinter printer)
        {
            Dictionary<string, int> ItemList = new Dictionary<string, int>(100);
            printer.SetLineSpacing(32);
            printer.SetAlignCenter();
            printer.WriteLine("MY SHOP",
                (byte)ThermalPrinter.PrintingStyle.DoubleHeight
                + (byte)ThermalPrinter.PrintingStyle.DoubleWidth);
            printer.WriteLine("My address, CITY");
            printer.LineFeed();
            printer.LineFeed();

            ItemList.Add("Item #1", 8990);
            ItemList.Add("Item #2 goes here", 2000);
            ItemList.Add("Item #3", 1490);
            ItemList.Add("Item number four", 490);
            ItemList.Add("Item #5 is cheap", 245);
            ItemList.Add("Item #6", 2990);
            ItemList.Add("The seventh item", 790);

            int total = 0;
            foreach (KeyValuePair<string, int> item in ItemList)
            {
                CashRegister(printer, item.Key, item.Value);
                total += item.Value;
            }

            printer.HorizontalLine(32);

            double dTotal = Convert.ToDouble(total) / 100;
            double VAT = 10.0;

            printer.WriteLine(String.Format("{0:0.00}", (dTotal)).PadLeft(32));

            printer.WriteLine("VAT 10,0%" + String.Format("{0:0.00}", (dTotal * VAT / 100)).PadLeft(23));

            printer.WriteLine(String.Format("$ {0:0.00}", dTotal * VAT / 100 + dTotal).PadLeft(16),
                ThermalPrinter.PrintingStyle.DoubleWidth);

            printer.LineFeed();

            printer.WriteLine("CASH" + String.Format("{0:0.00}", (double)total / 100).PadLeft(28));
            printer.LineFeed();
            printer.LineFeed();
            printer.SetAlignCenter();
            printer.WriteLine("Have a good day.", ThermalPrinter.PrintingStyle.Bold);

            printer.LineFeed();
            printer.SetAlignLeft();
            printer.WriteLine("Seller : Bob");
            printer.WriteLine("09-28-2011 10:53 02331 509");
            printer.PaperCut();

            // printer.LineFeed();
            // printer.LineFeed();
            // printer.LineFeed();
        }

        private static void CashRegister(ThermalPrinter printer, string item, int price)
        {
            printer.Reset();
            printer.Indent(0);

            if (item.Length > 24)
            {
                item = item.Substring(0, 23) + ".";
            }

            printer.WriteToBuffer(item.ToUpper());
            printer.Indent(25);
            string sPrice = String.Format("{0:0.00}", (double)price / 100);

            sPrice = sPrice.PadLeft(7);

            printer.WriteLine(sPrice);
            printer.Reset();
        }

        private static void TestBarcode(ThermalPrinter printer)
        {
            ThermalPrinter.BarcodeType myType = ThermalPrinter.BarcodeType.ean13;
            string myData = "3350030103392";
            printer.WriteLine(myType.ToString() + ", data: " + myData);

            // printer.SetLargeBarcode(true);
            // printer.LineFeed();
            // printer.PrintBarcode(myType, myData);
            printer.SetLargeBarcode(false);
            printer.LineFeed();
            printer.PrintBarcode(myType, myData);
        }
    }
}
