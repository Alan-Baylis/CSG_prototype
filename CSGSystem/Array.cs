using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace CSGSystem
{
    class TVector<T>
    {
        T[] v;
        int v_high = -1,
            v_min = -1,
            v_max = -1;
        int reserve = 10;
        public object GetData()//используется в render для отрисовки
        {
            return v;
        }
        public void Sort()
        {
            if(v_high>-1)
                Array.Sort(v, 0, v_high + 1);
        }
        bool NeedResize(int new_high)
        {
            return (new_high < v_min || new_high > v_max);
        }
        public TVector() { }
        public TVector(int use_reserve)
        {
            reserve = use_reserve;
        }
        public int Length
        {
            set { SetHigh(value - 1); }
            get { return v_high + 1; }
        }
        public int High
        {
            set { SetHigh(value); }
            get { return v_high; }
        }
        public T this[int i]
        {
            get { Debug.Assert(i >= 0 && i <= v_high); return v[i]; }
            set { v[i] = value; }
        }
        void SetHigh(int new_high)
        {
            Debug.Assert(new_high >= -1);
            int r = (new_high * 50) / 100 + reserve;
            v_max = new_high + r;
            v_min = new_high - r;
            if (v_min < -1) v_min = -1;
            T[] new_arr = new T[v_max + 1];
            for (int i = 0; i <= v_high && i <= new_high; i++)
                new_arr[i] = v[i];
            v = new_arr;
            v_high = new_high;
        }
        public void Push(T value)
        {
            Add(value);
        }
        public void Add()
        {
            if (NeedResize(v_high + 1)) SetHigh(v_high + 1);
            else v_high += 1;
        }
        public void Pop(int count)
        {
            if (NeedResize(v_high - count)) SetHigh(v_high - count);
            else v_high -= count;
        }
        public T Pop()
        {
            T result = v[v_high];
            if (NeedResize(v_high - 1)) SetHigh(v_high - 1);
            else v_high -= 1;
            return result;
        }
        public void Add(T value)
        {
            if (NeedResize(v_high + 1)) SetHigh(v_high + 1);
            else v_high += 1;
            v[v_high] = value;
        }
        public void Add(TVector<T> value)
        {
            int t = v_high;
            if (NeedResize(v_high + value.v_high + 1)) SetHigh(v_high + value.v_high + 1);
            else v_high = v_high + value.v_high + 1;
            for (int i = 0; i <= value.v_high; i++)
                v[t + 1 + i] = value.v[i];
        }
        public void Del(int index)
        {
            Debug.Assert(index >= 0 && index <= v_high);
            if (index != v_high)
                v[index] = v[v_high];
            if (NeedResize(v_high - 1)) SetHigh(v_high - 1);
            else v_high--;
        }
        //public void DelWithShift(int index)
        //{
        //    Debug.Assert(index >= 0 && index <= v_high);
        //    for (int i = index; i < v_high; i++)
        //        v[i] = v[i + 1];
        //    if (NeedResize(v_high - 1)) SetHigh(v_high - 1);
        //    else v_high--;
        //}
    }


    class TIndexedVector<T>
    {
        TVector<T> v;
        TVector<int> free_v;
        void GetNewSpace()
        {
            v.Add();
            free_v.Add(v.High);
        }
        public TIndexedVector()
        {
            v = new TVector<T>();
            free_v = new TVector<int>();
        }
        public TIndexedVector(int use_reserve)
        {
            v = new TVector<T>(use_reserve);
            free_v = new TVector<int>(use_reserve);
        }
        public int New()
        {
            if (free_v.High < 0)
                GetNewSpace();
            return free_v.Pop();
        }
        public void Del(int id)
        {
            Debug.Assert(id >= 0);
            free_v.Add(id);
        }
        public T this[int i]
        {
            get { return v[i]; }
            set { v[i] = value; }
        }
    }
}
