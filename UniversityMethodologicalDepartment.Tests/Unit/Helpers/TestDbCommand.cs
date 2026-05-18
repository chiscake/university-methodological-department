using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace UniversityMethodologicalDepartment.Tests.Unit.Helpers;

/// <summary>
/// Минимальная in-memory реализация <see cref="DbCommand"/> для тестов интерцепторов EF Core.
/// Поддерживает чтение/запись <see cref="DbCommand.CommandText"/>; остальные члены не реализованы.
/// </summary>
internal sealed class TestDbCommand : DbCommand
{
    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection { get; } = new TestDbParameterCollection();
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }

    public override int ExecuteNonQuery() => 0;

    public override object? ExecuteScalar() => null;

    public override void Prepare() { }

    protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        => throw new NotSupportedException();

    private sealed class TestDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _list = new();

        public override int Count => _list.Count;
        public override object SyncRoot => this;

        public override int Add(object value)
        {
            _list.Add((DbParameter)value);
            return _list.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                _list.Add((DbParameter)value);
            }
        }

        public override void Clear() => _list.Clear();

        public override bool Contains(object value) => _list.Contains((DbParameter)value);

        public override bool Contains(string value) => _list.Any(p => p.ParameterName == value);

        public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)_list).CopyTo(array, index);

        public override System.Collections.IEnumerator GetEnumerator() => _list.GetEnumerator();

        public override int IndexOf(object value) => _list.IndexOf((DbParameter)value);

        public override int IndexOf(string parameterName) => _list.FindIndex(p => p.ParameterName == parameterName);

        public override void Insert(int index, object value) => _list.Insert(index, (DbParameter)value);

        public override void Remove(object value) => _list.Remove((DbParameter)value);

        public override void RemoveAt(int index) => _list.RemoveAt(index);

        public override void RemoveAt(string parameterName)
        {
            var i = IndexOf(parameterName);
            if (i >= 0) _list.RemoveAt(i);
        }

        protected override DbParameter GetParameter(int index) => _list[index];

        protected override DbParameter GetParameter(string parameterName)
            => _list.First(p => p.ParameterName == parameterName);

        protected override void SetParameter(int index, DbParameter value) => _list[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var i = IndexOf(parameterName);
            if (i >= 0) _list[i] = value;
            else _list.Add(value);
        }
    }
}
