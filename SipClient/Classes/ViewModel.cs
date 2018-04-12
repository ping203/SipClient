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
            set { _badgeValue = value; NotifyPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Notify

        //        private string _selectedItem;

        //        private ObservableCollection<string> _items = new ObservableCollection<string>();

        //        public IEnumerable Items
        //        {
        //            get { return _items; }
        //        }

        //        public string SelectedItem
        //        {
        //            get { return _selectedItem; }
        //            set
        //            {
        //                _selectedItem = value;
        //#pragma warning disable CS7036 // There is no argument given that corresponds to the required formal parameter 'e' of 'PropertyChangedEventHandler'
        //#pragma warning disable CS7036 // There is no argument given that corresponds to the required formal parameter 'e' of 'PropertyChangedEventHandler'
        //                OnPropertyChanged("SelectedItem");
        //#pragma warning restore CS7036 // There is no argument given that corresponds to the required formal parameter 'e' of 'PropertyChangedEventHandler'
        //#pragma warning restore CS7036 // There is no argument given that corresponds to the required formal parameter 'e' of 'PropertyChangedEventHandler'
        //            }
        //        }

        //        public string NewItem
        //        {
        //            set
        //            {
        //                if (SelectedItem != null)
        //                {
        //                    return;
        //                }
        //                if (!string.IsNullOrEmpty(value))
        //                {
        //                    _items.Add(value);
        //                    SelectedItem = value;
        //                }
        //            }
        //        }

        #endregion
    }
}
