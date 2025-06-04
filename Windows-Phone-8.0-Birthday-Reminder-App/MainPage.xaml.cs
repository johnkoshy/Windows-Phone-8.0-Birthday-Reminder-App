using System;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Phone.Scheduler;

namespace Windows_Phone_8._0_Birthday_Reminder_App
{
    public partial class MainPage : PhoneApplicationPage
    {
        private ObservableCollection<Birthday> _birthdays = new ObservableCollection<Birthday>();
        private const string BIRTHDAYS_KEY = "Birthdays";

        public MainPage()
        {
            InitializeComponent();
            DataContext = this;
            BirthdayList.ItemsSource = _birthdays;
            LoadBirthdays();
        }

        public ObservableCollection<Birthday> Birthdays
        {
            get { return _birthdays; }
        }

        private void LoadBirthdays()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(BIRTHDAYS_KEY))
            {
                var savedBirthdays = (List<Birthday>)settings[BIRTHDAYS_KEY];
                foreach (var birthday in savedBirthdays)
                {
                    _birthdays.Add(birthday);
                    ScheduleReminder(birthday);
                }
            }
        }

        private void AddBirthdayButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddBirthdayPage.xaml", UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (NavigationContext.QueryString.ContainsKey("NewBirthday"))
            {
                string name = NavigationContext.QueryString["Name"];
                DateTime date = DateTime.Parse(NavigationContext.QueryString["Date"]);
                var birthday = new Birthday { Name = name, Date = date };
                _birthdays.Add(birthday);
                SaveBirthdays();
                ScheduleReminder(birthday);
            }
        }

        private void SaveBirthdays()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            settings[BIRTHDAYS_KEY] = _birthdays.ToList();
            settings.Save();
        }
        private void CheckRemindersButton_Click(object sender, RoutedEventArgs e)
{
    var reminders = ScheduledActionService.GetActions<Reminder>();
    
    if (!reminders.Any())  // Better than Count() == 0
    {
        MessageBox.Show("No reminders scheduled.");
        return;
    }

    // Build reminder list using string.Format
    string reminderList = "Scheduled Reminders:\n";
    foreach (var reminder in reminders)
    {
        reminderList += string.Format("- {0} at {1}\n", reminder.Name, reminder.BeginTime);
    }
    
    MessageBox.Show(reminderList);
}
        private void ScheduleReminder(Birthday birthday)
        {
            string reminderName = "BirthdayReminder_" + birthday.Name;
            ScheduledActionService.Remove(reminderName);

            // TEST SETTINGS (1 minute from now)
            DateTime testTime = DateTime.Now.AddMinutes(1);
            DateTime testExpireTime = testTime.AddHours(1);

            var reminder = new Reminder(reminderName)
            {
                Title = "BIRTHDAY REMINDER",
                Content = string.Format("{0}'s birthday today!", birthday.Name),
                BeginTime = testTime,
                ExpirationTime = testExpireTime,
                RecurrenceType = RecurrenceInterval.None, // Important for testing
                NavigationUri = new Uri("/MainPage.xaml", UriKind.Relative)
            };

            ScheduledActionService.Add(reminder);
            MessageBox.Show(string.Format("Reminder set for {0} at {1}", birthday.Name, testTime));
        }

        private void AddTestReminder_Click(object sender, RoutedEventArgs e)
        {
            var testReminder = new Reminder("TEST_REMINDER_123")
            {
                Title = "TEST REMINDER",
                Content = "This is a test notification",
                BeginTime = DateTime.Now.AddMinutes(1),
                ExpirationTime = DateTime.Now.AddMinutes(2),
                RecurrenceType = RecurrenceInterval.None
            };

            ScheduledActionService.Add(testReminder);
            MessageBox.Show("Test reminder added for 1 minute from now");
        }

    }

    public class Birthday
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}