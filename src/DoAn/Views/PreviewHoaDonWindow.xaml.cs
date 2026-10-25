using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DoAn.Views
{
    public partial class PreviewHoaDonWindow : Window
    {
        private readonly FlowDocument _doc;
        private readonly string _maHoaDon;

        public PreviewHoaDonWindow(FlowDocument doc, string maHoaDon = "HoaDon")
        {
            InitializeComponent();
            _doc = doc;
            _maHoaDon = maHoaDon;
            docReader.Document = _doc;
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Định dạng lại khổ giấy vừa khít với trang in PDF
                _doc.PageHeight = printDialog.PrintableAreaHeight;
                _doc.PageWidth = printDialog.PrintableAreaWidth;
                _doc.ColumnWidth = printDialog.PrintableAreaWidth;

                IDocumentPaginatorSource idpSource = _doc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, $"HoaDon_{_maHoaDon}");

                MessageBox.Show("Đã xuất hóa đơn thành công!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}