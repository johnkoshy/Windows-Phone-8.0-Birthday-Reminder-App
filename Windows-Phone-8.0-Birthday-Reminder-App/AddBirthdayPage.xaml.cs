using System;
using System.Windows;
using Microsoft.Phone.Controls;

namespace Windows_Phone_8._0_Birthday_Reminder_App
{
    public partial class AddBirthdayPage : PhoneApplicationPage
    {
        public AddBirthdayPage()
        {
            InitializeComponent();
            DatePicker.Value = DateTime.Now;
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Please enter a name.");
                return;
            }

            string uri = String.Format("/MainPage.xaml?Name={0}&Date={1}", Uri.EscapeDataString(NameTextBox.Text), DatePicker.Value.Value.ToString("o"));
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }
    }
}