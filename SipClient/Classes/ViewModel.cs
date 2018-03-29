using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SipClient.Classes
{
    public class ViewModel : INotifyPropertyChanged
    {
        private string _badgeValue;

        public string BadgeValue
        {
            get { return _badgeValue; }
            set { _badgeValue = value;  NotifyPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Notify

        private string _selectedItem;

        private ObservableCollection<string> _items = new ObservableCollection<string>();

        public IEnumerable Items
        {
            get { return _items; }
        }

        public string SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                //OnPropertyChanged("SelectedItem");
            }
        }

        public string NewItem
        {
            set
            {
                if (SelectedItem != null)
                {
                    return;
                }
                if (!string.IsNullOrEmpty(value))
                {
                    _items.Add(value);
                    SelectedItem = value;
                }
            }
        }

        //protected void OnPropertyChanged(string propertyName)
        //{
        //    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        //public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
