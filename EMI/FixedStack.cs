using System.Threading;
using System.Threading.Tasks;

/*
 * 21.06.2022 - создано и полность отестировано в ручную
 */

/// <summary>
/// Асинхронный потокобезопасный стек фиксированного размера (когда он заполнен или пуст - просто ожидает)
/// </summary>
/// <typeparam name="T">Тип который будет содержаться в стеке</typeparam>
internal class FixedStack<T>
{
    /// <summary>
    /// Индекс последней свободной ячейки (если 0 то пуст если равен размеру стека - значит заполнен)
    /// </summary>
    private volatile int FreeIndex = 0;
    /// <summary>
    /// Элементы стека
    /// </summary>
    private readonly T[] Items;
    /// <summary>
    /// Сколько элементов находить в стеке
    /// </summary>
    public int Count => FreeIndex;
    /// <summary>
    /// Сколько максимум может находиться элементов в стеке
    /// </summary>
    public int Size => Items.Length;

    /// <summary>
    /// Инициализировать новый стек
    /// </summary>
    /// <param name="size">размер стека</param>
    public FixedStack(int size)
    {
        Items = new T[size];
    }

    /// <summary>
    /// Добавляет элемент в конец стека элемент (если стек переполнен, будет ожидать)
    /// </summary>
    /// <param name="item">предмет который будет добавлен</param>
    /// <param name="token">токен отмены операции</param>
    public async Task Push(T item, CancellationToken token)
    {
    //ожидаем если стек заполнен
    reWait: while (!token.IsCancellationRequested && FreeIndex == Items.Length)
            await Task.Yield();

        if (token.IsCancellationRequested)
            return;

        lock (Items)
        {
            //если стек опять переполнился
            if (FreeIndex == Items.Length)
                goto reWait;

            Items[FreeIndex++] = item;
        }
    }

    /// <summary>
    /// Извлекает и возвращает последний элемент из стека (если стек пуст, будет ожидать)
    /// </summary>
    /// <param name="token">токен отмены операции (вернёт default значение)</param>
    /// <returns>элемент из стека</returns>
    public async Task<T> Pop(CancellationToken token)
    {
    //ожидаем если стек пуст
    reWait: while (!token.IsCancellationRequested && FreeIndex == 0)
            await Task.Yield();

        if (token.IsCancellationRequested)
            return default;

        lock (Items)
        {
            //если кто то уже забрал предмет
            if (FreeIndex == 0)
                goto reWait;

            return Items[--FreeIndex];
        }
    }
}