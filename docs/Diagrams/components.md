# Пункт 2. Component Diagram

Тот же состав узлов и связей в PlantUML: [components.puml](components.puml).

## Компонентная схема артефактов сборки (EXE/DLL)

```mermaid
flowchart LR
    EXE["UniversityMethodologicalDepartment.exe"]
    APP["UniversityMethodologicalDepartment.App.dll"]
    BUS["UniversityMethodologicalDepartment.Bus.dll"]

    WINUI["Microsoft.WindowsAppSDK.dll / WinUI runtime"]
    MVVM["CommunityToolkit.Mvvm.dll"]
    EF["Microsoft.EntityFrameworkCore.dll"]
    NP["Npgsql.EntityFrameworkCore.PostgreSQL.dll"]
    SB["Supabase.dll"]
    OXML["DocumentFormat.OpenXml.dll"]
    CHARTS["LiveChartsCore.SkiaSharpView.WinUI.dll"]
    CFG["Microsoft.Extensions.Configuration*.dll"]
    DI["Microsoft.Extensions.DependencyInjection.dll"]

    SUPABASE["Supabase Auth + Edge Functions"]
    PG["PostgreSQL"]
    SQL["supabase/migrations\n(base + rls + audit + triggers)"]

    EXE --> APP
    APP --> BUS

    APP --> WINUI
    APP --> MVVM
    APP --> OXML
    APP --> CHARTS
    APP --> SB
    APP --> CFG
    APP --> DI

    BUS --> EF
    BUS --> NP
    BUS --> SB
    BUS --> CFG

    APP --> SUPABASE
    BUS --> SUPABASE
    BUS --> PG
    SUPABASE --> PG
    SQL --> PG
```

## Пояснение

В данной диаграмме верхний уровень представлен исполняемым файлом `UniversityMethodologicalDepartment.exe`, который загружает прикладной модуль `UniversityMethodologicalDepartment.App.dll`. Прикладной модуль использует модуль доменной и data-логики `UniversityMethodologicalDepartment.Bus.dll`, а также основные внешние библиотеки из NuGet: WinUI runtime, MVVM toolkit, OpenXML, LiveCharts, EF Core, Npgsql, Supabase и компоненты конфигурации/DI.
