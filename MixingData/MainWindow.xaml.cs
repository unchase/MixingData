using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
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
    public partial class MainWindow : Window
    {
        public PersonContext db;

        public MainWindow()
        {
            InitializeComponent();

            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Программа запущена";

            //через эту переменную (контекст данных) будет осуществляться связь и работа с БД
            db = new PersonContext();

            try
            {
                // загружаем данные из БД
                db.Persons.Load();
                db.NewPersons.Load();
                db.Keys.Load();

                // устанавливаем привязку к кэшу
                MixingDataPersonalDataDataGrid.ItemsSource = db.Persons.Local.ToBindingList();
                MixingDataDepersonalizeDataDataGrid.ItemsSource = db.NewPersons.Local.ToBindingList();
                MixingDataKeysDataGrid.ItemsSource = db.Keys.Local.ToBindingList();

                this.Closing += MainWindow_Closing;
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
            db.Dispose();
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
                        db.Persons.Remove(person);
                    }
                    else
                    {
                        j++;
                    }
                }
            }
            db.SaveChanges();

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
            if (MixingDataPersonalDataDataGrid.Items != null)
            {
                var persons = new List<Person>();
                for (var i = 0; i < db.Persons.Local.Count; i++)
                {
                    if (db.Persons.Local[i] == null) continue;
                    persons.Add(db.Persons.Local[i]);
                }
                db.Persons.Local.Clear();
                db.SaveChanges();
                foreach (var person in persons)
                {
                    if (person.Id == new Guid())
                        person.Id = Guid.NewGuid();
                    db.Persons.Local.Add(person);
                }
            }
            db.SaveChanges();

            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Изменения в базе данных сохранены";
        }

        private void MixingDataDepersonalizeButton_Click(object sender, RoutedEventArgs e)
        {
            //запоминаем в переменных: splitCount - количество подмножеств исходного множества персональных данных, shift - циклический сдвиг значений перемешиваемых аттрибутов внутри каждого подмножества
            var splitCount = 1;
            var shift = 1;
            if (!int.TryParse(MixingDataSplitOnTextBox.Text, out splitCount))
                return;
            if (!int.TryParse(MixingDataShiftOnTextBox.Text, out shift))
                return;

            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Процедура обезличивания персональных данных запущена";

            //задаем набор позиций, определяющих процедуру обезличивания персональных данных
            TNetwork.Position pa1 = new TNetwork.Position(true, 1);
            TNetwork.Position pa2 = new TNetwork.Position(true, 1);
            TNetwork.Position pa3 = new TNetwork.Position(true, 1);
            TNetwork.Position pa4 = new TNetwork.Position(true, 1);
            TNetwork.Position pa5 = new TNetwork.Position(true, 1);
            TNetwork.Position pb1 = new TNetwork.Position(true, 1);
            TNetwork.Position pb2 = new TNetwork.Position(true, 1);
            TNetwork.Position pb3 = new TNetwork.Position(true, 1);
            TNetwork.Position pb4 = new TNetwork.Position(true, 1);
            TNetwork.Position pb5 = new TNetwork.Position(true, 1);
            TNetwork.Position pZapObez = new TNetwork.Position(true, 1);
            TNetwork.Position pZ1 = new TNetwork.Position(false, 0);
            TNetwork.Position pZ2 = new TNetwork.Position(false, 0);
            TNetwork.Position pZ3 = new TNetwork.Position(false, 0);
            TNetwork.Position pZ4 = new TNetwork.Position(false, 0);
            TNetwork.Position p1 = new TNetwork.Position(false, 0);
            TNetwork.Position p3 = new TNetwork.Position(false, 0);
            TNetwork.Position p4 = new TNetwork.Position(false, 0);
            TNetwork.Position p6 = new TNetwork.Position(false, 0);
            TNetwork.Position p10 = new TNetwork.Position(false, 0);
            TNetwork.Position p11 = new TNetwork.Position(false, 0);
            TNetwork.Position p9 = new TNetwork.Position(false, 0);
            TNetwork.Position p12 = new TNetwork.Position(false, 0);
            TNetwork.Position p13 = new TNetwork.Position(false, 0);
            TNetwork.Position p14 = new TNetwork.Position(false, 0);
            TNetwork.Position pKonecObez = new TNetwork.Position(false, 0);

            //задаем набор переходов с формированием Fl и Fr, определяющий процедуру обезличивания персональных данных
            TNetwork.Translation t2 = new TNetwork.Translation(new List<TNetwork.Position>() { pZapObez }, new List<TNetwork.Position>() { pZ1, pZ2, pZ3, pZ4 });
            TNetwork.Translation t3 = new TNetwork.Translation(new List<TNetwork.Position>() { pa1, pa2, pa3 }, new List<TNetwork.Position>() { p1 });
            TNetwork.Translation t5 = new TNetwork.Translation(new List<TNetwork.Position>() { pa4, pa5 }, new List<TNetwork.Position>() { p6 });
            TNetwork.Translation t6 = new TNetwork.Translation(new List<TNetwork.Position>() { pb1, pb2, pb3 }, new List<TNetwork.Position>() { p4 });
            TNetwork.Translation t8 = new TNetwork.Translation(new List<TNetwork.Position>() { pb4, pb5 }, new List<TNetwork.Position>() { p3 });
            TNetwork.Translation t9 = new TNetwork.Translation(new List<TNetwork.Position>() { p1, p3 }, new List<TNetwork.Position>() { p10, p11 });
            TNetwork.Translation t11 = new TNetwork.Translation(new List<TNetwork.Position>() { p4, p6 }, new List<TNetwork.Position>() { p9, p12 });
            TNetwork.Translation t14 = new TNetwork.Translation(new List<TNetwork.Position>() { p11 }, new List<TNetwork.Position>() { p13 });
            TNetwork.Translation t15 = new TNetwork.Translation(new List<TNetwork.Position>() { p12 }, new List<TNetwork.Position>() { p14 });
            TNetwork.Translation t16 = new TNetwork.Translation(new List<TNetwork.Position>() { p13, p14 }, new List<TNetwork.Position>() { pKonecObez });

            //переменные для хранения записей исходной таблицы персональных данных, которые будем между собой перемешивать
            Person person1;
            Person person2;

            //переменные для записи в таблицу обезличенных персональных данных после выполнения перемешивания
            NewPerson newPerson1;
            NewPerson newPerson2;

            //переменная для записи ключей для деобезличивания в таблицу с ключами после выполнения перемешивания
            Key key1;
            //Key key2;

            //очищаем таблицы с обезличенными персональными данными и с ключами для обезличивания, сохранив все изменения в БД, перед обезличиванием
            ClearDepersonalizeDataGridAndKeysDataGrid();

            //запоминаем в N - число записей в таблице персональных данных
            var N = db.Persons.Local.Count;
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

            //p - номер первой записи в каждом подмножестве
            int p;
            Person firstPerson;

            //создаем временный список с записями персональных данных и инициализируем его
            List<Person> tempPersons = new List<Person>();
            foreach (var person in db.Persons.Local)
            {
                tempPersons.Add(person);
            }
            //shift раз осуществляем циклический сдвиг на 1 элемент влево в этом временном массиве

            //осуществляем циклический сдвиг на shift в каждом подмножестве множества элементов персональных данных и результат помещаем в новый список (уже со сдвигом элементов) персональных данных
            #region это алгоритм смещения элементов множества на shift влево
            int last; //будет хранить номер последнего элемента в каждом подмножестве
            for (int i = 0; i < splitCount; i++)
            {
                p = k * i; //номер первой записи в каждом подмножестве
                //shift раз сдвигаем влево на 1 элемент
                for (int s = 0; s < shift; s++)
                {
                    //запоминаем первый элемент подмножества
                    firstPerson = tempPersons[p];
                    last = p + k - 1;
                    //смещаем все элементы подмножества на 1 влево
                    for (int j = p; j < p + k; j++)
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
            for (int i = 0; i < tempPersons.Count; i++)
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
                person1 = db.Persons.Local[i];
                person2 = tempPersons[i];

                //задаем наборы значений аттрибутов двух записей в таблице персональных данных, передаваемых Т-сети
                List<string> firstPersonAttributes = new List<string>();
                firstPersonAttributes.Add(person1.Id.ToString());
                firstPersonAttributes.Add(person1.LastName);
                firstPersonAttributes.Add(person1.FirstName);
                firstPersonAttributes.Add(person1.Patronymic);
                firstPersonAttributes.Add(person1.DateOfBirth.ToString("{0:dd.MM.yyyy}"));
                firstPersonAttributes.Add(person1.Address);
                List<string> secondPersonAttributes = new List<string>();
                secondPersonAttributes.Add(person2.Id.ToString());
                secondPersonAttributes.Add(person2.LastName);
                secondPersonAttributes.Add(person2.FirstName);
                secondPersonAttributes.Add(person2.Patronymic);
                secondPersonAttributes.Add(person2.DateOfBirth.ToString("{0:dd.MM.yyyy}"));
                secondPersonAttributes.Add(person2.Address);

                //создаем экземпляр класса, реализующего Т-сеть
                TNetwork tNet = new TNetwork(firstPersonAttributes, secondPersonAttributes);

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
                    newPerson2 = new NewPerson() { Id = new Guid(tNet.secondAttributValues[0]), LastName = tNet.secondAttributValues[1], FirstName = tNet.secondAttributValues[2], Patronymic = tNet.secondAttributValues[3], DateOfBirth = DateTime.ParseExact(tNet.secondAttributValues[4], "{0:dd.MM.yyyy}", new CultureInfo("ru-RU")), Address = tNet.secondAttributValues[5] };
                    db.NewPersons.Local.Add(newPerson2);

                    //создаем записи добавляем их в таблицу с ключами для деобезличивания
                    key1 = new Key() { Id = Guid.NewGuid(), Key1 = new Guid(tNet.firstAttributValues[0]), Key2 = newPerson2.Id };
                    db.Keys.Local.Add(key1);

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
            db.SaveChanges();
        }

        public void ClearDepersonalizeDataGridAndKeysDataGrid()
        {
            //очищаем таблицу с обезличенными персональными данными перед обезличиванием
            db.NewPersons.Local.Clear();

            //очищаем таблицу с ключами для деобезличивания перед обезличиванием
            db.Keys.Local.Clear();

            //сохраняем все изменения в БД перед запуском процедуры обезличивания
            db.SaveChanges();

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
            MessageBox.Show("Данная программа реализует сетевую модель обезличивания персональных данных методом перемешивания.\nАвтор: Н.Чеботов", "О программе ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MixingDataCancelDepersonalizeButton_Click(object sender, RoutedEventArgs e)
        {
            //меняем фон у StatusBar'а и выводим в него сообщение
            MixingDataMessagesStatusBar.Background = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            MixingDataMessagesTextBlock.Text = "Процедура деобезличивания персональных данных запущена";

            //задаем набор позиций, определяющих процедуру обезличивания персональных данных
            TNetwork.Position pZapDeobez = new TNetwork.Position(true, 1);
            TNetwork.Position p9 = new TNetwork.Position(true, 1);
            TNetwork.Position p10 = new TNetwork.Position(true, 1);
            TNetwork.Position p7 = new TNetwork.Position(false, 0);
            TNetwork.Position p8 = new TNetwork.Position(false, 0);
            TNetwork.Position p2 = new TNetwork.Position(false, 0);
            TNetwork.Position p5 = new TNetwork.Position(false, 0);
            TNetwork.Position pa1 = new TNetwork.Position(false, 0);
            TNetwork.Position pa2 = new TNetwork.Position(false, 0);
            TNetwork.Position pa3 = new TNetwork.Position(false, 0);
            TNetwork.Position pa4 = new TNetwork.Position(false, 0);
            TNetwork.Position pa5 = new TNetwork.Position(false, 0);
            TNetwork.Position pb1 = new TNetwork.Position(false, 0);
            TNetwork.Position pb2 = new TNetwork.Position(false, 0);
            TNetwork.Position pb3 = new TNetwork.Position(false, 0);
            TNetwork.Position pb4 = new TNetwork.Position(false, 0);
            TNetwork.Position pb5 = new TNetwork.Position(false, 0);
            TNetwork.Position pK1 = new TNetwork.Position(false, 0);
            TNetwork.Position pK2 = new TNetwork.Position(false, 0);
            TNetwork.Position pKonecDeobez = new TNetwork.Position(false, 0);

            //задаем набор переходов с формированием Fl и Fr, определяющий процедуру обезличивания персональных данных
            TNetwork.Translation t13 = new TNetwork.Translation(new List<TNetwork.Position>() { p7, p8 }, new List<TNetwork.Position>() { p9, p10, pZapDeobez });
            TNetwork.Translation t10 = new TNetwork.Translation(new List<TNetwork.Position>() { p2 }, new List<TNetwork.Position>() { p7 });
            TNetwork.Translation t12 = new TNetwork.Translation(new List<TNetwork.Position>() { p5 }, new List<TNetwork.Position>() { p8 });
            TNetwork.Translation t7 = new TNetwork.Translation(new List<TNetwork.Position>() { pb1, pb2, pb3, pb4, pb5, pK2 }, new List<TNetwork.Position>() { p5 });
            TNetwork.Translation t4 = new TNetwork.Translation(new List<TNetwork.Position>() { pa1, pa2, pa3, pa4, pa5, pK1 }, new List<TNetwork.Position>() { p2 });
            TNetwork.Translation t1 = new TNetwork.Translation(new List<TNetwork.Position>() { pKonecDeobez }, new List<TNetwork.Position>() { pK1, pK2 });

            NewPerson newPerson1;
            NewPerson newPerson2;

            Person person1;

            //очищаем таблицу записей с персональными данными
            db.Persons.Local.Clear();

            //для каждого набора ключей для деобезличивания восстанавливаем персональные данные
            foreach (var key in db.Keys.Local)
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

                newPerson1 = db.NewPersons.Find(key.Key1);
                newPerson2 = db.NewPersons.Find(key.Key2);

                //задаем наборы значений аттрибутов двух записей в таблице персональных данных, передаваемых Т-сети
                List<string> firstPersonAttributes = new List<string>();
                firstPersonAttributes.Add(newPerson1.Id.ToString());
                firstPersonAttributes.Add(newPerson1.LastName);
                firstPersonAttributes.Add(newPerson1.FirstName);
                firstPersonAttributes.Add(newPerson1.Patronymic);
                firstPersonAttributes.Add(newPerson1.DateOfBirth.ToString("{0:dd.MM.yyyy}"));
                firstPersonAttributes.Add(newPerson1.Address);
                List<string> secondPersonAttributes = new List<string>();
                secondPersonAttributes.Add(newPerson2.Id.ToString());
                secondPersonAttributes.Add(newPerson2.LastName);
                secondPersonAttributes.Add(newPerson2.FirstName);
                secondPersonAttributes.Add(newPerson2.Patronymic);
                secondPersonAttributes.Add(newPerson2.DateOfBirth.ToString("{0:dd.MM.yyyy}"));
                secondPersonAttributes.Add(newPerson2.Address);

                //создаем экземпляр класса, реализующего Т-сеть
                TNetwork tNet = new TNetwork(firstPersonAttributes, secondPersonAttributes);

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
                    person1 = new Person() { Id = new Guid(tNet.firstAttributValues[0]), LastName = tNet.firstAttributValues[1], FirstName = tNet.firstAttributValues[2], Patronymic = tNet.firstAttributValues[3], DateOfBirth = DateTime.ParseExact(tNet.firstAttributValues[4], "{0:dd.MM.yyyy}", new CultureInfo("ru-RU")), Address = tNet.firstAttributValues[5] };
                    db.Persons.Local.Add(person1);

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
            db.SaveChanges();
        }

        private void MixingDataClearPersonDataButton_Click(object sender, RoutedEventArgs e)
        {
            //очищаем таблицу с персональными данными
            db.Persons.Local.Clear();

            //сохраняем изменения в БД
            db.SaveChanges();

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
            String result = (string)Clipboard.GetData(DataFormats.UnicodeText);
            try
            {
                StreamWriter sw = new StreamWriter("export.txt");
                sw.WriteLine(result);
                sw.Close();
                Process.Start("export.txt");
            }
            catch (Exception ex)
            { }
        }
    }
}
