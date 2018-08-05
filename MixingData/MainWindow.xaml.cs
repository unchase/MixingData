using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MixingData.Database;
using MixingData.Database.Entities;
using MixingData.Depersonalization;
using Key = MixingData.Database.Entities.Key;

namespace MixingData
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public PersonContext Db;

        public MainWindow()
        {
            InitializeComponent();

            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Программа запущена";

            //через эту переменную (контекст данных) будет осуществляться связь и работа с БД
            Db = new PersonContext();

            try
            {
                // загружаем данные из БД
                Db.Persons.Load();
                Db.NewPersons.Load();
                Db.Keys.Load();

                // устанавливаем привязку к кэшу
                MixingDataPersonalDataDataGrid.ItemsSource = Db.Persons.Local.ToBindingList();
                MixingDataDepersonalizeDataDataGrid.ItemsSource = Db.NewPersons.Local.ToBindingList();
                MixingDataKeysDataGrid.ItemsSource = Db.Keys.Local.ToBindingList();

                Closing += MainWindow_Closing;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.InnerException != null)
                    MessageBox.Show(ex.InnerException.Message + "\n" + ex.InnerException.InnerException.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //освобождает контекст данных из памяти
            Db.Dispose();
        }

        //удаляет выбранные элементы из БД
        private void MixingDataDeletePersonDataButton_Click(object sender, RoutedEventArgs e)
        {
            if(MixingDataPersonalDataDataGrid.SelectedItems != null)
            {
                var count = MixingDataPersonalDataDataGrid.SelectedItems.Count;
                var j = 0;
                for (var i = 0; i < count; i++)
                {
                    var person = MixingDataPersonalDataDataGrid.SelectedItems[j] as Person;
                    if (person != null)
                    {
                        Db.Persons.Remove(person);
                    }
                    else
                    {
                        j++;
                    }
                }
            }
            Db.SaveChanges();

            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Удаление выбранных записей из базы данных завершено";
        }

        private void MixingDataSplitOnTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if ((MixingDataSplitOnTextBox.Text == "" && e.Key == System.Windows.Input.Key.D0) ||
                !System.Text.RegularExpressions.Regex.IsMatch(e.Key.ToString(), @"\d+"))
                e.Handled = true;
        }

        private void MixingDataShiftOnTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if ((MixingDataShiftOnTextBox.Text == "" && e.Key == System.Windows.Input.Key.D0) ||
                !System.Text.RegularExpressions.Regex.IsMatch(e.Key.ToString(), @"\d+"))
                e.Handled = true;
        }

        //сохраняет несохраненные изменения в БД
        private void MixingDataUpdatePersonDataButton_Click(object sender, RoutedEventArgs e)
        {
            var persons = new List<Person>();
            for (var i = 0; i < Db.Persons.Local.Count; i++)
            {
                if (Db.Persons.Local[i] == null) continue;
                persons.Add(Db.Persons.Local[i]);
            }
            Db.Persons.Local.Clear();
            Db.SaveChanges();
            foreach (var person in persons)
            {
                if (person.Id == new Guid())
                    person.Id = Guid.NewGuid();
                Db.Persons.Local.Add(person);
            }
            
            Db.SaveChanges();

            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Изменения в базе данных сохранены";
        }

        private void MixingDataDepersonalizeButton_Click(object sender, RoutedEventArgs e)
        {
            //запоминаем в переменных: splitCount - количество подмножеств исходного множества персональных данных, shift - циклический сдвиг значений перемешиваемых аттрибутов внутри каждого подмножества
            if (!int.TryParse(MixingDataSplitOnTextBox.Text, out var splitCount))
                return;
            if (!int.TryParse(MixingDataShiftOnTextBox.Text, out var shift))
                return;

            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Процедура обезличивания персональных данных запущена";

            //задаем набор позиций, определяющих процедуру обезличивания персональных данных
            var pa1 = new TNetwork.Position(true, 1);
            var pa2 = new TNetwork.Position(true, 1);
            var pa3 = new TNetwork.Position(true, 1);
            var pa4 = new TNetwork.Position(true, 1);
            var pa5 = new TNetwork.Position(true, 1);
            var pb1 = new TNetwork.Position(true, 1);
            var pb2 = new TNetwork.Position(true, 1);
            var pb3 = new TNetwork.Position(true, 1);
            var pb4 = new TNetwork.Position(true, 1);
            var pb5 = new TNetwork.Position(true, 1);
            var pZapObez = new TNetwork.Position(true, 1);
            var pZ1 = new TNetwork.Position(false, 0);
            var pZ2 = new TNetwork.Position(false, 0);
            var pZ3 = new TNetwork.Position(false, 0);
            var pZ4 = new TNetwork.Position(false, 0);
            var p1 = new TNetwork.Position(false, 0);
            var p3 = new TNetwork.Position(false, 0);
            var p4 = new TNetwork.Position(false, 0);
            var p6 = new TNetwork.Position(false, 0);
            var p10 = new TNetwork.Position(false, 0);
            var p11 = new TNetwork.Position(false, 0);
            var p9 = new TNetwork.Position(false, 0);
            var p12 = new TNetwork.Position(false, 0);
            var p13 = new TNetwork.Position(false, 0);
            var p14 = new TNetwork.Position(false, 0);
            var pKonecObez = new TNetwork.Position(false, 0);

            //задаем набор переходов с формированием Fl и Fr, определяющий процедуру обезличивания персональных данных
            var t2 = new TNetwork.Translation(new List<TNetwork.Position>() { pZapObez }, new List<TNetwork.Position>() { pZ1, pZ2, pZ3, pZ4 });
            var t3 = new TNetwork.Translation(new List<TNetwork.Position>() { pa1, pa2, pa3 }, new List<TNetwork.Position>() { p1 });
            var t5 = new TNetwork.Translation(new List<TNetwork.Position>() { pa4, pa5 }, new List<TNetwork.Position>() { p6 });
            var t6 = new TNetwork.Translation(new List<TNetwork.Position>() { pb1, pb2, pb3 }, new List<TNetwork.Position>() { p4 });
            var t8 = new TNetwork.Translation(new List<TNetwork.Position>() { pb4, pb5 }, new List<TNetwork.Position>() { p3 });
            var t9 = new TNetwork.Translation(new List<TNetwork.Position>() { p1, p3 }, new List<TNetwork.Position>() { p10, p11 });
            var t11 = new TNetwork.Translation(new List<TNetwork.Position>() { p4, p6 }, new List<TNetwork.Position>() { p9, p12 });
            var t14 = new TNetwork.Translation(new List<TNetwork.Position>() { p11 }, new List<TNetwork.Position>() { p13 });
            var t15 = new TNetwork.Translation(new List<TNetwork.Position>() { p12 }, new List<TNetwork.Position>() { p14 });
            var t16 = new TNetwork.Translation(new List<TNetwork.Position>() { p13, p14 }, new List<TNetwork.Position>() { pKonecObez });

            //очищаем таблицы с обезличенными персональными данными и с ключами для обезличивания, сохранив все изменения в БД, перед обезличиванием
            ClearDepersonalizeDataGridAndKeysDataGrid();

            //запоминаем в N - число записей в таблице персональных данных
            var N = Db.Persons.Local.Count;
            //считаем k - количество записей в каждом подмножестве
            int k;
            if (N % splitCount == 0)
            {
                k = N/splitCount;
            }
            else
            {
                k = N/splitCount + 1;
            }

            if (k < 2)
            {
                //меняем фон у StatusBar'а и выводим в него сообщение
                MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(201, 16, 18));
                MixingDataMessagesTextBlock.Text = "Невозможно разбить на " + splitCount.ToString() + " подмножеств";

                //если исключение возникло, то функция завершается досрочно
                return;
            }

            //создаем временный список с записями персональных данных и инициализируем его
            var tempPersons = new List<Person>();
            foreach (var person in Db.Persons.Local)
            {
                tempPersons.Add(person);
            }
            //shift раз осуществляем циклический сдвиг на 1 элемент влево в этом временном массиве

            //осуществляем циклический сдвиг на shift в каждом подмножестве множества элементов персональных данных и результат помещаем в новый список (уже со сдвигом элементов) персональных данных
            #region это алгоритм смещения элементов множества на shift влево

            for (var i = 0; i < splitCount; i++)
            {
                //p - номер первой записи в каждом подмножестве
                var p = k * i;
                //shift раз сдвигаем влево на 1 элемент
                for (var s = 0; s < shift; s++)
                {
                    //запоминаем первый элемент подмножества
                    var firstPerson = tempPersons[p];
                    var last = p + k - 1; //будет хранить номер последнего элемента в каждом подмножестве
                    //смещаем все элементы подмножества на 1 влево
                    for (var j = p; j < p + k; j++)
                    {
                        if (j + 1 < N)
                            tempPersons[j] = tempPersons[j + 1];
                        else
                        {
                            last = j;
                            break;
                        }
                    }
                    //на место последнего элемента подмножества ставим первый
                    tempPersons[last] = firstPerson;
                }
            }
            #endregion

            //для всех элементов нового списка персональных данных
            for (var i = 0; i < tempPersons.Count; i++)
            {
                //задаем начальную маркировку для набора позиций, определяющих процедуру обезличивания персональных данных
                pa1 = new TNetwork.Position(true, 1);
                pa2 = new TNetwork.Position(true, 1);
                pa3 = new TNetwork.Position(true, 1);
                pa4 = new TNetwork.Position(true, 1);
                pa5 = new TNetwork.Position(true, 1);
                pb1 = new TNetwork.Position(true, 1);
                pb2 = new TNetwork.Position(true, 1);
                pb3 = new TNetwork.Position(true, 1);
                pb4 = new TNetwork.Position(true, 1);
                pb5 = new TNetwork.Position(true, 1);
                pZapObez = new TNetwork.Position(true, 1);
                pZ1 = new TNetwork.Position(false, 0);
                pZ2 = new TNetwork.Position(false, 0);
                pZ3 = new TNetwork.Position(false, 0);
                pZ4 = new TNetwork.Position(false, 0);
                p1 = new TNetwork.Position(false, 0);
                p3 = new TNetwork.Position(false, 0);
                p4 = new TNetwork.Position(false, 0);
                p6 = new TNetwork.Position(false, 0);
                p10 = new TNetwork.Position(false, 0);
                p11 = new TNetwork.Position(false, 0);
                p9 = new TNetwork.Position(false, 0);
                p12 = new TNetwork.Position(false, 0);
                p13 = new TNetwork.Position(false, 0);
                p14 = new TNetwork.Position(false, 0);
                pKonecObez = new TNetwork.Position(false, 0);

                //задаем исходный набор переходов с формированием Fl и Fr, определяющий процедуру обезличивания персональных данных
                t2 = new TNetwork.Translation(new List<TNetwork.Position>() { pZapObez }, new List<TNetwork.Position>() { pZ1, pZ2, pZ3, pZ4 });
                t3 = new TNetwork.Translation(new List<TNetwork.Position>() { pa1, pa2, pa3 }, new List<TNetwork.Position>() { p1 });
                t5 = new TNetwork.Translation(new List<TNetwork.Position>() { pa4, pa5 }, new List<TNetwork.Position>() { p6 });
                t6 = new TNetwork.Translation(new List<TNetwork.Position>() { pb1, pb2, pb3 }, new List<TNetwork.Position>() { p4 });
                t8 = new TNetwork.Translation(new List<TNetwork.Position>() { pb4, pb5 }, new List<TNetwork.Position>() { p3 });
                t9 = new TNetwork.Translation(new List<TNetwork.Position>() { p1, p3 }, new List<TNetwork.Position>() { p10, p11 });
                t11 = new TNetwork.Translation(new List<TNetwork.Position>() { p4, p6 }, new List<TNetwork.Position>() { p9, p12 });
                t14 = new TNetwork.Translation(new List<TNetwork.Position>() { p11 }, new List<TNetwork.Position>() { p13 });
                t15 = new TNetwork.Translation(new List<TNetwork.Position>() { p12 }, new List<TNetwork.Position>() { p14 });
                t16 = new TNetwork.Translation(new List<TNetwork.Position>() { p13, p14 }, new List<TNetwork.Position>() { pKonecObez });

                //будем перемещивать аттрибуты исходных персональных данных и получившихся после смещения на shift в каждом подмножестве
                var person1 = Db.Persons.Local[i];
                var person2 = tempPersons[i];

                //задаем наборы значений аттрибутов двух записей в таблице персональных данных, передаваемых Т-сети
                var firstPersonAttributes = new List<string>();
                firstPersonAttributes.Add(person1.Id.ToString());
                firstPersonAttributes.Add(person1.LastName);
                firstPersonAttributes.Add(person1.FirstName);
                firstPersonAttributes.Add(person1.Patronymic);
                firstPersonAttributes.Add(person1.DateOfBirth.ToString("{0:dd.MM.yyyy}"));
                firstPersonAttributes.Add(person1.Address);
                var secondPersonAttributes = new List<string>
                {
                    person2.Id.ToString(),
                    person2.LastName,
                    person2.FirstName,
                    person2.Patronymic,
                    person2.DateOfBirth.ToString("{0:dd.MM.yyyy}"),
                    person2.Address
                };

                //создаем экземпляр класса, реализующего Т-сеть
                var tNet = new TNetwork(firstPersonAttributes, secondPersonAttributes);

                //меняем фон у StatusBar'а и выводим в него сообщение
                MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
                MixingDataMessagesTextBlock.Text = "Выполняется последовательность переходов t";

                //запускаем последовательно переходы в с сетевой моелью (Т-сеть) обезличивания персональных данных
                try
                {
                    t2.DoTranslate(false);
                    t3.DoTranslate(false);
                    t5.DoTranslate(false);
                    t6.DoTranslate(false);
                    t8.DoTranslate(false);
                    t9.DoTranslate(false);
                    t11.DoTranslate(false);
                    tNet.SwapAttrInPositions(4);
                    tNet.SwapAttrInPositions(5);
                    t14.DoTranslate(false);
                    t15.DoTranslate(false);
                    t16.DoTranslate(false);
                }
                //если при каком-то переходе возникнет исключение, то оно будет обработано
                catch (Exception ex)
                {
                    //меняем фон у StatusBar'а и выводим в него сообщение
                    MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(201, 16, 18));
                    MixingDataMessagesTextBlock.Text = "Возникло исключение: " + ex.Message;

                    //если исключение возникло, то функция завершается досрочно
                    return;
                }

                //если позиция конца обезличивания промаркирована, то
                if (pKonecObez.marked)
                {
                    //создаем записи добавляем их в таблицу с обезличенными персональными записями
                    var newPerson2 = new NewPerson() { Id = new Guid(tNet.secondAttributValues[0]), LastName = tNet.secondAttributValues[1], FirstName = tNet.secondAttributValues[2], Patronymic = tNet.secondAttributValues[3], DateOfBirth = DateTime.ParseExact(tNet.secondAttributValues[4], "{0:dd.MM.yyyy}", new CultureInfo("ru-RU")), Address = tNet.secondAttributValues[5] };
                    Db.NewPersons.Local.Add(newPerson2);

                    //создаем записи добавляем их в таблицу с ключами для деобезличивания
                    var key1 = new Key() { Id = Guid.NewGuid(), Key1 = new Guid(tNet.firstAttributValues[0]), Key2 = newPerson2.Id };
                    Db.Keys.Local.Add(key1);

                    //меняем фон у StatusBar'а и выводим в него сообщение
                    MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
                    MixingDataMessagesTextBlock.Text = "Процедура обезличивания персональных данных завершена";
                }
                else
                {
                    //меняем фон у StatusBar'а и выводим в него сообщение
                    MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(201, 16, 18));
                    MixingDataMessagesTextBlock.Text = "Позиция конца обезличивания не была промаркирована.";

                    //очищаем таблицы с обезличенными персональными данными и с ключами для обезличивания, сохранив все изменения в БД, перед обезличиванием
                    ClearDepersonalizeDataGridAndKeysDataGrid();

                    //если позиция конца обезличивания не помечена, то функция завершается досрочно
                    return;
                }
            }
            //сохраняем изменения в БД
            Db.SaveChanges();
        }

        public void ClearDepersonalizeDataGridAndKeysDataGrid()
        {
            //очищаем таблицу с обезличенными персональными данными перед обезличиванием
            Db.NewPersons.Local.Clear();

            //очищаем таблицу с ключами для деобезличивания перед обезличиванием
            Db.Keys.Local.Clear();

            //сохраняем все изменения в БД перед запуском процедуры обезличивания
            Db.SaveChanges();

            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Таблицы с обезличенными данными и ключами для деобезличивания очищены";
        }

        private void MixingDataClearDepersonalizeDataAndKeysDataGrid_Click(object sender, RoutedEventArgs e)
        {
            ClearDepersonalizeDataGridAndKeysDataGrid();
        }

        //закрывает главное окно
        private void MixingDataExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //выводит сообщение с информацией о программе
        private void MixingDataAboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Данная программа реализует сетевую модель обезличивания персональных данных методом перемешивания.\nАвтор: 'unchase'", "О программе ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MixingDataCancelDepersonalizeButton_Click(object sender, RoutedEventArgs e)
        {
            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Процедура деобезличивания персональных данных запущена";

            //задаем набор позиций, определяющих процедуру обезличивания персональных данных
            var pZapDeobez = new TNetwork.Position(true, 1);
            var p9 = new TNetwork.Position(true, 1);
            var p10 = new TNetwork.Position(true, 1);
            var p7 = new TNetwork.Position(false, 0);
            var p8 = new TNetwork.Position(false, 0);
            var p2 = new TNetwork.Position(false, 0);
            var p5 = new TNetwork.Position(false, 0);
            var pa1 = new TNetwork.Position(false, 0);
            var pa2 = new TNetwork.Position(false, 0);
            var pa3 = new TNetwork.Position(false, 0);
            var pa4 = new TNetwork.Position(false, 0);
            var pa5 = new TNetwork.Position(false, 0);
            var pb1 = new TNetwork.Position(false, 0);
            var pb2 = new TNetwork.Position(false, 0);
            var pb3 = new TNetwork.Position(false, 0);
            var pb4 = new TNetwork.Position(false, 0);
            var pb5 = new TNetwork.Position(false, 0);
            var pK1 = new TNetwork.Position(false, 0);
            var pK2 = new TNetwork.Position(false, 0);
            var pKonecDeobez = new TNetwork.Position(false, 0);

            //задаем набор переходов с формированием Fl и Fr, определяющий процедуру обезличивания персональных данных
            var t13 = new TNetwork.Translation(new List<TNetwork.Position>() { p7, p8 }, new List<TNetwork.Position>() { p9, p10, pZapDeobez });
            var t10 = new TNetwork.Translation(new List<TNetwork.Position>() { p2 }, new List<TNetwork.Position>() { p7 });
            var t12 = new TNetwork.Translation(new List<TNetwork.Position>() { p5 }, new List<TNetwork.Position>() { p8 });
            var t7 = new TNetwork.Translation(new List<TNetwork.Position>() { pb1, pb2, pb3, pb4, pb5, pK2 }, new List<TNetwork.Position>() { p5 });
            var t4 = new TNetwork.Translation(new List<TNetwork.Position>() { pa1, pa2, pa3, pa4, pa5, pK1 }, new List<TNetwork.Position>() { p2 });
            var t1 = new TNetwork.Translation(new List<TNetwork.Position>() { pKonecDeobez }, new List<TNetwork.Position>() { pK1, pK2 });

            //очищаем таблицу записей с персональными данными
            Db.Persons.Local.Clear();

            //для каждого набора ключей для деобезличивания восстанавливаем персональные данные
            foreach (var key in Db.Keys.Local)
            {
                //задаем исходную маркировку для набора позиций, определяющих процедуру обезличивания персональных данных
                pZapDeobez = new TNetwork.Position(true, 1);
                p9 = new TNetwork.Position(true, 1);
                p10 = new TNetwork.Position(true, 1);
                p7 = new TNetwork.Position(false, 0);
                p8 = new TNetwork.Position(false, 0);
                p2 = new TNetwork.Position(false, 0);
                p5 = new TNetwork.Position(false, 0);
                pa1 = new TNetwork.Position(false, 0);
                pa2 = new TNetwork.Position(false, 0);
                pa3 = new TNetwork.Position(false, 0);
                pa4 = new TNetwork.Position(false, 0);
                pa5 = new TNetwork.Position(false, 0);
                pb1 = new TNetwork.Position(false, 0);
                pb2 = new TNetwork.Position(false, 0);
                pb3 = new TNetwork.Position(false, 0);
                pb4 = new TNetwork.Position(false, 0);
                pb5 = new TNetwork.Position(false, 0);
                pK1 = new TNetwork.Position(false, 0);
                pK2 = new TNetwork.Position(false, 0);
                pKonecDeobez = new TNetwork.Position(false, 0);

                //задаем исходный набор переходов с формированием Fl и Fr, определяющий процедуру обезличивания персональных данных
                t13 = new TNetwork.Translation(new List<TNetwork.Position>() { p7, p8 }, new List<TNetwork.Position>() { p9, p10, pZapDeobez });
                t10 = new TNetwork.Translation(new List<TNetwork.Position>() { p2 }, new List<TNetwork.Position>() { p7 });
                t12 = new TNetwork.Translation(new List<TNetwork.Position>() { p5 }, new List<TNetwork.Position>() { p8 });
                t7 = new TNetwork.Translation(new List<TNetwork.Position>() { pb1, pb2, pb3, pb4, pb5, pK2 }, new List<TNetwork.Position>() { p5 });
                t4 = new TNetwork.Translation(new List<TNetwork.Position>() { pa1, pa2, pa3, pa4, pa5, pK1 }, new List<TNetwork.Position>() { p2 });
                t1 = new TNetwork.Translation(new List<TNetwork.Position>() { pKonecDeobez }, new List<TNetwork.Position>() { pK1, pK2 });

                var newPerson1 = Db.NewPersons.Find(key.Key1);
                var newPerson2 = Db.NewPersons.Find(key.Key2);

                //задаем наборы значений аттрибутов двух записей в таблице персональных данных, передаваемых Т-сети
                var firstPersonAttributes = new List<string>
                {
                    newPerson1.Id.ToString(),
                    newPerson1.LastName,
                    newPerson1.FirstName,
                    newPerson1.Patronymic,
                    newPerson1.DateOfBirth.ToString("{0:dd.MM.yyyy}"),
                    newPerson1.Address
                };
                var secondPersonAttributes = new List<string>
                {
                    newPerson2.Id.ToString(),
                    newPerson2.LastName,
                    newPerson2.FirstName,
                    newPerson2.Patronymic,
                    newPerson2.DateOfBirth.ToString("{0:dd.MM.yyyy}"),
                    newPerson2.Address
                };

                //создаем экземпляр класса, реализующего Т-сеть
                var tNet = new TNetwork(firstPersonAttributes, secondPersonAttributes);

                //меняем фон у StatusBar'а и выводим в него сообщение
                MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
                MixingDataMessagesTextBlock.Text = "Выполняется последовательность переходов t";

                //запускаем последовательно переходы в с сетевой моелью (Т-сеть) деобезличивания персональных данных
                try
                {
                    t13.DoTranslate(true);
                    t10.DoTranslate(true);
                    t12.DoTranslate(true);
                    t7.DoTranslate(true);
                    t4.DoTranslate(true);
                    tNet.SwapAttrInPositions(4);
                    tNet.SwapAttrInPositions(5);
                    t1.DoTranslate(true);
                }
                //если при каком-то переходе возникнет исключение, то оно будет обработано
                catch (Exception ex)
                {
                    //меняем фон у StatusBar'а и выводим в него сообщение
                    MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(201, 16, 18));
                    MixingDataMessagesTextBlock.Text = "Возникло исключение: " + ex.Message;

                    //если исключение возникло, то функция завершается досрочно
                    return;
                }

                //если позиция конца обезличивания промаркирована, то
                if (pKonecDeobez.marked)
                {
                    //создаем записи добавляем их в таблицу с обезличенными персональными записями
                    var person1 = new Person() { Id = new Guid(tNet.firstAttributValues[0]), LastName = tNet.firstAttributValues[1], FirstName = tNet.firstAttributValues[2], Patronymic = tNet.firstAttributValues[3], DateOfBirth = DateTime.ParseExact(tNet.firstAttributValues[4], "{0:dd.MM.yyyy}", new CultureInfo("ru-RU")), Address = tNet.firstAttributValues[5] };
                    Db.Persons.Local.Add(person1);

                    //меняем фон у StatusBar'а и выводим в него сообщение
                    MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
                    MixingDataMessagesTextBlock.Text = "Процедура деобезличивания персональных данных завершена";
                }
                else
                {
                    //меняем фон у StatusBar'а и выводим в него сообщение
                    MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(201, 16, 18));
                    MixingDataMessagesTextBlock.Text = "Позиция конца деобезличивания не была промаркирована.";

                    //если позиция конца обезличивания не помечена, то функция завершается досрочно (при этом деобезличенные ранее данные не стираются)
                    return;
                }
            }
            //сохраняем изменения в БД
            Db.SaveChanges();
        }

        private void MixingDataClearPersonDataButton_Click(object sender, RoutedEventArgs e)
        {
            //очищаем таблицу с персональными данными
            Db.Persons.Local.Clear();

            //сохраняем изменения в БД
            Db.SaveChanges();

            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Таблица с персональными данными очищена";
        }

        private void MixingDataSaveDepersonalizeDataDataGrid_Click(object sender, RoutedEventArgs e)
        {
            MixingDataDepersonalizeDataDataGrid.SelectAllCells();
            MixingDataDepersonalizeDataDataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            ApplicationCommands.Copy.Execute(null, MixingDataDepersonalizeDataDataGrid);
            MixingDataDepersonalizeDataDataGrid.UnselectAllCells();
            var result = (string)Clipboard.GetData(DataFormats.UnicodeText);
            try
            {
                var sw = new StreamWriter("export.txt");
                sw.WriteLine(result);
                sw.Close();
                Process.Start("export.txt");
            }
            catch (Exception ex)
            { }
        }
    }
}
