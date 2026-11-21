using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace HRM.Migrations;

/// <summary>Keeps compact handwritten migrations bound to EF's columns/constraints overload.</summary>
internal static class MigrationBuilderExtensions
{
    public static CreateTableBuilder<TColumns> CreateTable<TColumns>(
        this MigrationBuilder migrationBuilder,
        string name,
        Func<ColumnsBuilder, TColumns> columns,
        Action<CreateTableBuilder<TColumns>> constraints) =>
        migrationBuilder.CreateTable(name: name, columns: columns, constraints: constraints);
}
