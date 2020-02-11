using FixedWidthFileUtils;
using FixedWidthFileUtils.Serializers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestDeserializeWFFile()
        {
            string testFile = @"C:\temp\pos_AP_20200207211501810.txt";
            PositivePayFile file = null;
            using (FileStream fs = new FileStream(testFile, FileMode.Open))
            {
                file = FixedWidthSerializer.Deserialize<PositivePayFile>(fs);
            }
        }
    }

    #region MODELS
    public class PositivePayFile
    {
        [FixedField(0)]
        public FileHeader Header { get; set; }

        [FixedField(1)]
        public CheckGroup[] CheckGroups { get; set; }
    }
    public class FileHeader
    {
        [FixedField(0, 3)]
        public string Start => "*03";

        [FixedField(1, 5)]
        public int BankID { get; set; }

        [FixedField(2, 15)]
        public long AccountNumber { get; set; }

        [FixedField(3, 1)]
        public int AlwaysZero => 0;

        [FixedField(4, 61, ' ')]
        public string Spacing => string.Empty;
    }
    public class CheckRecord
    {
        [FixedField(0, 10)]
        public long CheckSerial { get; set; }

        [FixedField(1, 6)]
        [FixedFieldSerializer(typeof(WellsFargoDateSerializer))]
        public DateTime IssueDate { get; set; }

        [FixedField(2, 15)]
        public long AccountNumber { get; set; }

        [FixedField(3, 3)]
        public int TransactionCode => 320;

        [FixedField(4, 10)]
        [FixedFieldSerializer(typeof(DecimalToPenniesSerializer))]
        public decimal Amount { get; set; }

        private string _Payee;
        [FixedField(5, 41, ' ', FixedFieldAlignment.Left)]
        public string Payee
        {
            get => _Payee?.Trim();
            set => _Payee = value;
        }
    }
    public class CheckGroup
    {
        [FixedField(0)]
        public CheckRecord[] Records { get; set; }

        [FixedField(1)]
        public CheckGroupTrailer Trailer { get; set; }
    }
    public class CheckGroupTrailer
    {
        [FixedField(0, 15, ' ', FixedFieldAlignment.Left)]
        public string Start => "&";

        [FixedField(1, 5)]
        public int RecordCount { get; set; }

        [FixedField(3, 10)]
        [FixedFieldSerializer(typeof(DecimalToPenniesSerializer))]
        public decimal TotalAmount { get; set; }

        [FixedField(2, 3, ' ')]
        [FixedField(4, 47, ' ')]
        public string Spacer => string.Empty;
    }
    #endregion

    #region CUSTOM SERIALIZER
    public class WellsFargoDateSerializer : FixedFieldSerializer<DateTime>
    {
        public override DateTime Deserialize(string input) => DateTime.ParseExact(input, "MMddyy", CultureInfo.InvariantCulture);
        public override string Serialize(DateTime input) => input.ToString("MMddyy");
    }
    #endregion
}
