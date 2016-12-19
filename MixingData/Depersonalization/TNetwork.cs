using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MixingData.Depersonalization
{
    public class TNetwork
    {
        //определяет первый набор аттрибутов
        public List<string> firstAttributValues;
        //определяет второй набор аттрибутов
        public List<string> secondAttributValues;

        #region Конструкторы класса
        public TNetwork() : this(new List<string>(), new List<string>()) { }
        public TNetwork(List<string> fAttrValues, List<string> sAttrValues)
        {
            firstAttributValues = fAttrValues;
            secondAttributValues = sAttrValues;
        }
        #endregion

        public class Position
        {
            //определяет, помечена ли данная вершина
            public bool marked;
            //определяет количество меток данной вершины
            public int markedCount = 0;

            #region Конструкторы класса
            public Position() : this(false, 0) { }
            public Position(bool m, int mCount)
            {
                marked = m;
                markedCount = mCount;
            }
            #endregion
        }

        public class Translation
        {
            //задают некоторое число позиций относительно данного перехода слева и справа соответственно
            public List<Position> Fl;
            public List<Position> Fr;

            #region Конструкторы класса
            public Translation() : this(new List<Position>(), new List<Position>()){ }
            public Translation(List<Position> fl, List<Position> fr)
            {
                Fl = fl;
                Fr = fr;
            }
            #endregion

            //функция проверяет, разрешен ли на запуск данный переход слева(справа)
            public bool TranslationAccess(bool right)
            {
                return right ? Fr.All(rightPosition => rightPosition.markedCount >= Fold(rightPosition, this, true)) : Fl.All(leftPosition => leftPosition.markedCount >= Fold(leftPosition, this, false));
            }

            //функция осуществляет данный переход слева(справа)
            public void DoTranslate(bool right)
            {
                //если переход слева(справа) разрешен
                if (TranslationAccess(right))
                {
                    //если осуществляем переход слева
                    if (!right)
                    {
                        //определяем новую маркировку для позиций во множестве Fr
                        foreach (var rightPosition in Fr)
                        {
                            rightPosition.markedCount = rightPosition.markedCount - Fold(rightPosition, this, false) + Fold(rightPosition, this, true);
                            rightPosition.marked = rightPosition.markedCount > 0;
                        }
                        //определяем новую маркировку для позиций во множестве Fl
                        foreach (var leftPosition in Fl)
                        {
                            leftPosition.markedCount = leftPosition.markedCount - Fold(leftPosition, this, false) + Fold(leftPosition, this, true);
                            leftPosition.marked = leftPosition.markedCount > 0;
                        }
                    }
                    else //осуществляем переход справа
                    {
                        //определяем новую маркировку для позиций во множестве Fr
                        foreach (var rightPosition in Fr)
                        {
                            rightPosition.markedCount = rightPosition.markedCount - Fold(rightPosition, this, true) + Fold(rightPosition, this, false);
                            rightPosition.marked = rightPosition.markedCount > 0;
                        }
                        //определяем новую маркировку для позиций во множестве Fl
                        foreach (var leftPosition in Fl)
                        {
                            leftPosition.markedCount = leftPosition.markedCount - Fold(leftPosition, this, true) + Fold(leftPosition, this, false);
                            leftPosition.marked = leftPosition.markedCount > 0;
                        }
                    }
                }
                else
                {
                    //выдаем исключение, если данный переход не разрешен
                    throw new Exception("Данный переход не разрешен");
                }
            }
        }

        //функция возвращает кратность позиции p относительно перехода t слева(справа), то есть число появлений позиции в комплекте перехода
        public static int Fold(Position p, Translation t, bool right)
        {
            return right ? t.Fr.Count(x => x == p) : t.Fl.Count(x => x == p);
        }

        //функция меняет местами значения аттрибутов, заданных индексом
        public void SwapAttrInPositions(int attrIndex)
        {
            var temp = firstAttributValues[attrIndex];
            firstAttributValues[attrIndex] = secondAttributValues[attrIndex];
            secondAttributValues[attrIndex] = temp;
        }
    }
}
