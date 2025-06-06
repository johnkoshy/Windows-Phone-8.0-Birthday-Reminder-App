using System;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Phone.Scheduler;
using System.Diagnostics;

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
            // 1. Debug: Show the parsed date
            MessageBox.Show(string.Format("Parsed date: {0}", birthday.Date));

            // 2. Create safe reminder name
            string reminderName = "BirthdayReminder_" + Uri.EscapeDataString(birthday.Name);

            // 3. Check if reminder already exists (debug)
            var exists = ScheduledActionService.Find(reminderName) != null;
            MessageBox.Show("Reminder exists before removal: " + exists);

            // Remove existing reminder if any
            ScheduledActionService.Remove(reminderName);

            // Create new reminder
            var reminder = new Reminder(reminderName)
            {
                Title = "Birthday Reminder",
                Content = string.Format("{0}'s birthday today!", birthday.Name),
                BeginTime = new DateTime(DateTime.Now.Year, birthday.Date.Month, birthday.Date.Day, 8, 0, 0),
                ExpirationTime = new DateTime(DateTime.Now.Year, birthday.Date.Month, birthday.Date.Day, 23, 59, 59),
                RecurrenceType = RecurrenceInterval.Yearly,
                NavigationUri = new Uri("/MainPage.xaml", UriKind.Relative)
            };

            // 4. Handle past dates
            if (reminder.BeginTime < DateTime.Now)
            {
                MessageBox.Show(string.Format(
                    "Adjusting date from {0} to {1}",
                    reminder.BeginTime,
                    reminder.BeginTime.AddYears(1)));

                reminder.BeginTime = reminder.BeginTime.AddYears(1);
                reminder.ExpirationTime = reminder.ExpirationTime.AddYears(1);
            }

            // Debug: Show final reminder details
            MessageBox.Show(string.Format(
                "Final Reminder Details:\n" +
                "Name: {0}\n" +
                "Title: {1}\n" +
                "Content: {2}\n" +
                "Begin: {3}\n" +
                "Expires: {4}\n" +
                "Recurrence: {5}",
                reminder.Name,
                reminder.Title,
                reminder.Content,
                reminder.BeginTime,
                reminder.ExpirationTime,
                reminder.RecurrenceType));

            // Add the reminder
            ScheduledActionService.Add(reminder);

            // 5. Verify reminder was added
            exists = ScheduledActionService.Find(reminderName) != null;
            MessageBox.Show("Reminder exists after adding: " + exists);
        }

        private void AddTestReminder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string reminderName = "TEST_REMINDER_123";

                // Remove any existing reminder first
                if (ScheduledActionService.Find(reminderName) != null)
                {
                    ScheduledActionService.Remove(reminderName);
                }

                var testReminder = new Reminder(reminderName)
                {
                    Title = "TEST REMINDER",
                    Content = "This is a test notification",
                    BeginTime = DateTime.Now.AddMinutes(1),
                    ExpirationTime = DateTime.Now.AddMinutes(2),
                    RecurrenceType = RecurrenceInterval.None,
                    NavigationUri = new Uri("/MainPage.xaml", UriKind.Relative)
                };

                ScheduledActionService.Add(testReminder);
                MessageBox.Show("Test reminder added for 1 minute from now");
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("General error: " + ex.Message);
            }
        }

    }

    public class Birthday
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}