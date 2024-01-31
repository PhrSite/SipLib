/////////////////////////////////////////////////////////////////////////////////////
//  File:   ThreadSafeGenericList.cs                                29 Aug 23 PHR
/////////////////////////////////////////////////////////////////////////////////////

namespace SipLib.Collections;

/// <summary>
/// This class is a thread-safe, generic (template based) List class.
/// 
/// This class does not support enumerating the list elements or an index operator. To enumerate the
/// current items in the list or to get an element at an index position, call the ToArray() method
/// then perform the desired action on the array of current items.
/// </summary>
/// <typeparam name="T">The Type of the objects in the List.</typeparam>
public class ThreadSafeGenericList<T>
{
    private List<T> m_List = new List<T>();
    private object m_LockObj = new object();

    /// <summary>
    /// Adds a new item to the list.
    /// </summary>
    /// <param name="NewItem"></param>
    public void Add(T NewItem)
    {
        lock (m_LockObj)
        {
            m_List.Add(NewItem);
        }
    }

    /// <summary>
    /// Gets the number of items in the list.
    /// </summary>
    /// <value></value>
    public int Count
    {
        get
        {
            int Result;
            lock (m_LockObj)
            {
                Result = m_List.Count;
            }

            return Result;
        }
    }

    /// <summary>
    /// Clears the list.
    /// </summary>
    public void Clear()
    {
        lock (m_LockObj)
        {
            m_List.Clear();
        }
    }

    /// <summary>
    /// Removes an element from the list if its present
    /// </summary>
    /// <param name="Element"></param>
    public void Remove(T Element)
    {
        lock (m_LockObj)
        {
            if (m_List.Contains(Element) == true)
                m_List.Remove(Element);
        }
    }

    /// <summary>
    /// Converts the list to an array.
    /// </summary>
    /// <returns>Returns a new array containing all of the items</returns>
    public T[] ToArray()
    {
        T[] Result = null;
        lock (m_LockObj)
        {
            Result = m_List.ToArray();
        }

        return Result;
    }
}

