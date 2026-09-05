---
name: avalonia-api
description: Look up Avalonia binding, control, or template patterns when a XerahS UI task needs API guidance.
metadata:
  keywords:
    - avalonia
    - axaml
    - xaml
    - ui
    - mvvm
    - communitytoolkit
    - databinding
    - controls
    - styling
    - templates
    - cross-platform
    - dependency-properties
    - attached-properties
    - contextmenu
    - contextflyout
    - menuflyout
    - fluentavalonia
    - navigation
    - container-queries
version: 1.3.0
last_updated: 2026-03-16
---

# Avalonia UI Framework - Complete API & Best Practices Guide

> **Target Framework**: .NET 10.0+  
> **File Extension**: `.axaml` (Avalonia XAML)  
> **Official Docs**: https://docs.avaloniaui.net/

---

## Table of Contents

1. [AXAML Fundamentals](#axaml-fundamentals)
2. [Controls & UI Elements](#controls--ui-elements)
3. [Layout System](#layout-system)
4. [Data Binding](#data-binding)
5. [MVVM Pattern with CommunityToolkit.Mvvm](#mvvm-pattern-with-communitytoolkitmvvm)
6. [Styling & Theming](#styling--theming)
7. [Dependency & Attached Properties](#dependency--attached-properties)
8. [Custom Controls](#custom-controls)
9. [Control Templates](#control-templates)
10. [Resources & Converters](#resources--converters)
11. [Events & Commands](#events--commands)
12. [Navigation](#navigation)
13. [Cross-Platform Patterns](#cross-platform-patterns)
14. [Performance & Best Practices](#performance--best-practices)
15. [Developer Tools](#developer-tools)
16. [Common Mistakes to Avoid](#common-mistakes-to-avoid)
17. [Common Patterns in XerahS](#common-patterns-in-xerahs)

---

## AXAML Fundamentals

### File Structure

Every `.axaml` file follows this standard structure:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:YourApp.ViewModels"
        x:Class="YourApp.Views.MainWindow"
        x:DataType="vm:MainViewModel"
        x:CompileBindings="True">
    
    <!-- Content here -->
    
</Window>
```

### Required Namespace Declarations

| Namespace | Purpose | Required |
|-----------|---------|----------|
| `xmlns="https://github.com/avaloniaui"` | Core Avalonia controls | ✅ Always |
| `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` | XAML language features | ✅ Always |
| `xmlns:vm="using:YourNamespace.ViewModels"` | ViewModel references | ⚠️ For MVVM |
| `xmlns:local="using:YourNamespace"` | Local types/controls | 🔹 As needed |

### Custom Namespace Syntax

```xml
<!-- Current assembly -->
<xmlns:myAlias1="using:AppNameSpace.MyNamespace">

<!-- Referenced assembly (library) -->
<xmlns:myAlias2="clr-namespace:OtherAssembly.MyNameSpace;assembly=OtherAssembly">

<!-- Alternative using: prefix (Avalonia style) -->
<xmlns:controls="using:XerahS.UI.Controls">
```

### Control Content vs. Attributes

```xml
<!-- Using Content property (implicit) -->
<Button>Hello World!</Button>

<!-- Using Content attribute (explicit) -->
<Button Content="Hello World!" />

<!-- Using property element syntax -->
<Button>
    <Button.Content>
        <StackPanel>
            <TextBlock Text="Complex" />
            <TextBlock Text="Content" />
        </StackPanel>
    </Button.Content>
</Button>
```

---

## Controls & UI Elements

### Common Built-in Controls

#### Input Controls
- **TextBox**: Single/multi-line text input
- **PasswordBox**: Masked password input
- **NumericUpDown**: Numeric input with increment/decrement
- **CheckBox**: Boolean toggle
- **RadioButton**: Mutually exclusive selection
- **Slider**: Continuous range selection
- **ComboBox**: Dropdown selection
- **AutoCompleteBox**: Text input with suggestions
- **DatePicker**: Date selection
- **TimePicker**: Time selection
- **ColorPicker**: Color selection

#### Display Controls
- **TextBlock**: Read-only text display
- **Label**: Text with access key support
- **Image**: Display images
- **Border**: Visual border around content
- **ContentControl**: Single content container

#### Layout Panels
- **Panel**: Basic container (fills available space)
- **StackPanel**: Vertical/horizontal stack
- **Grid**: Row/column grid layout
- **DockPanel**: Edge-docked layout
- **Canvas**: Absolute positioning
- **WrapPanel**: Wrapping flow layout
- **RelativePanel**: Relative positioning
- **UniformGrid**: Equal-sized cells

#### Lists & Collections
- **ListBox**: Selectable list
- **ListView**: List with view customization
- **TreeView**: Hierarchical tree
- **DataGrid**: Tabular data with columns
- **ItemsControl**: Base collection display
- **ItemsRepeater**: Virtualizing collection

#### Containers
- **Window**: Top-level window
- **UserControl**: Reusable UI component
- **ScrollViewer**: Scrollable content
- **Expander**: Collapsible content
- **TabControl**: Tabbed interface
- **SplitView**: Hamburger menu pattern

#### Buttons
- **Button**: Standard button
- **ToggleButton**: Two-state button
- **RepeatButton**: Auto-repeating button
- **RadioButton**: Mutually exclusive button
- **SplitButton**: Button with dropdown
- **DropDownButton**: Dropdown menu button

#### Advanced
- **Carousel**: Cycling content display
- **MenuFlyout**: Modern flyout-based context menu (⚠️ **Use this with FluentAvalonia**)
- **ContextFlyout**: Right-click menu container (⚠️ **Preferred over ContextMenu**)
- **ContextMenu**: Legacy right-click menu (⚠️ **Avoid with FluentAvalonia theme**)
- **Menu**: Menu bar
- **ToolTip**: Hover information
- **Flyout**: Popup overlay
- **Calendar**: Calendar display

---

## Layout System

### ScrollViewer Activation Requirements

A `ScrollViewer` only activates (shows and enables the scrollbar) when it receives a **finite** (bounded) height constraint from its parent during the Measure pass. If any ancestor passes `∞` (infinity) down the chain, the `ScrollViewer` will never scroll.

**Common sources of infinite height in XerahS layouts and their fixes:**

| Root cause | Why it breaks scrolling | Fix |
|---|---|---|
| `SplitView` as two-column shell | Inherits `ContentControl`; default `VerticalContentAlignment=Top` → `ContentPresenter` passes `∞` height | Replace with `Grid ColumnDefinitions="auto,*"` |
| `TransitioningContentControl` as page host | Internal animation `Panel` passes `∞` height during measure | Replace with `ContentControl HorizontalContentAlignment="Stretch" VerticalContentAlignment="Stretch"` |
| `TabControl` without `VerticalContentAlignment="Stretch"` | Inner `ContentPresenter` templates to `{TemplateBinding VerticalContentAlignment}`; default `Top` → `∞` down to tab bodies | Add `VerticalContentAlignment="Stretch"` to the `TabControl` |
| `ScrollViewer Padding="N"` | Shrinks the *viewport* but does NOT add `N` to the scroll extent — bottom `N`px of content is permanently unreachable | Remove `Padding` from `ScrollViewer`; add `Margin="N"` to the inner `StackPanel`/`Grid` instead |

**Canonical scrollable settings page pattern:**

```xml
<!-- ✅ Correct: padding lives inside the scroll extent -->
<TabControl VerticalContentAlignment="Stretch">
    <TabItem Header="General">
        <ScrollViewer>
            <StackPanel Spacing="24" Margin="24">
                <!-- content -->
            </StackPanel>
        </ScrollViewer>
    </TabItem>
</TabControl>

<!-- ❌ Wrong: padding cuts the viewport, last Npx of content unreachable -->
<TabControl>  <!-- default VerticalContentAlignment=Top → ∞ height -->
    <TabItem Header="General">
        <ScrollViewer Padding="24">  <!-- bottom 24px permanently cut off -->
            <StackPanel Spacing="24">
            </StackPanel>
        </ScrollViewer>
    </TabItem>
</TabControl>
```

### Layout Process

Avalonia uses a two-pass layout system:

1. **Measure Pass**: Determines desired size of each control
2. **Arrange Pass**: Positions controls within available space

```
Control → Measure → MeasureOverride → DesiredSize
       → Arrange → ArrangeOverride → FinalSize
```

### Panel Comparison

| Panel | Use Case | Performance | Complexity |
|-------|----------|-------------|------------|
| **Panel** | Fill available space | ⚡ Best | Simple |
| **StackPanel** | Linear stack | ⚡ Good | Simple |
| **Canvas** | Absolute positioning | ⚡ Good | Simple |
| **DockPanel** | Edge docking | ✅ Good | Medium |
| **Grid** | Complex layouts | ⚠️ Moderate | Complex |
| **RelativePanel** | Relative constraints | ⚠️ Moderate | Complex |

**Recommendation**: Use `Panel` instead of `Grid` with no rows/columns for better performance.

### Common Layout Properties

```xml
<Control Width="100"                    <!-- Fixed width -->
         Height="50"                     <!-- Fixed height -->
         MinWidth="50"                   <!-- Minimum width -->
         MaxWidth="200"                  <!-- Maximum width -->
         Margin="10,5,10,5"              <!-- Left,Top,Right,Bottom -->
         Padding="5"                     <!-- Uniform padding -->
         HorizontalAlignment="Stretch"   <!-- Left|Center|Right|Stretch -->
         VerticalAlignment="Center"      <!-- Top|Center|Bottom|Stretch -->
         HorizontalContentAlignment="Center"  <!-- For content within -->
         VerticalContentAlignment="Center" />
```

### Grid Layout

```xml
<Grid RowDefinitions="Auto,*,50"           <!-- Rows: auto-size, fill, fixed 50 -->
      ColumnDefinitions="200,*,Auto">      <!-- Cols: 200, fill, auto-size -->
    
    <TextBlock Grid.Row="0" Grid.Column="0" Text="Header" />
    <Border Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="3" />
    
    <!-- Star sizing for proportions -->
    <Grid ColumnDefinitions="*,2*,*">  <!-- 1:2:1 ratio -->
        <!-- ... -->
    </Grid>
</Grid>
```

### DockPanel Layout

```xml
<DockPanel LastChildFill="True">
    <Menu DockPanel.Dock="Top" />
    <StatusBar DockPanel.Dock="Bottom" />
    <TreeView DockPanel.Dock="Left" Width="200" />
    
    <!-- Last child fills remaining space -->
    <ContentControl Content="{Binding CurrentView}" />
</DockPanel>
```

### StackPanel Layout

```xml
<StackPanel Orientation="Vertical"    <!-- Vertical|Horizontal -->
            Spacing="10">              <!-- Space between items -->
    <TextBlock Text="Item 1" />
    <TextBlock Text="Item 2" />
    <TextBlock Text="Item 3" />
</StackPanel>
```

### GridSplitter (Resizable Panes)

When a `Grid` has distinct content regions (sidebar + main, top/bottom split), add a `GridSplitter` so users can resize the panes. Dedicate a narrow column/row (3–6 px) for the splitter.

```xml
<!-- Two-pane resizable layout -->
<Grid ColumnDefinitions="250, 4, *">
    <TreeView Grid.Column="0" />
    <GridSplitter Grid.Column="1" ResizeDirection="Columns" />
    <ContentControl Grid.Column="2" Content="{Binding Detail}" />
</Grid>
```

**Key rules**:
- `ResizeDirection` must match the axis: `Columns` for a column splitter, `Rows` for a row splitter.
- Set `MinWidth`/`MaxWidth` (or `MinHeight`/`MaxHeight`) on adjacent cells to prevent collapsing to zero.
- Use `GridSplitter` whenever a Grid has two or more sizeable content regions.

### Responsive Layouts

Avalonia provides four approaches. Prefer these over fixed-pixel layouts.

#### Container Queries (preferred for reusable components)

Respond to the size of an ancestor control — not the window. Works live as the control resizes.

```xml
<Border Container.Name="main" Container.Sizing="Width">
    <Panel.Styles>
        <ContainerQuery Name="main" Query="max-width:600">
            <Style Selector="StackPanel#sidebar">
                <Setter Property="IsVisible" Value="False" />
            </Style>
        </ContainerQuery>
    </Panel.Styles>
    <!-- content here -->
</Border>
```

- Combine conditions: `Query="min-width:400 and max-width:800"`
- `Container.Sizing`: `Width`, `Height`, or `Width Height`

#### OnFormFactor (static platform detection)

Resolves once at startup. Use for desktop-vs-mobile differences that don't respond to window resizing.

```xml
<Grid ColumnDefinitions="{OnFormFactor Desktop='250,*', Mobile='*'}">
    <Border IsVisible="{OnFormFactor Desktop=True, Mobile=False}" />
</Grid>
```

#### Reflowing Panels (self-adapting)

```xml
<ItemsRepeater ItemsSource="{Binding Items}">
    <ItemsRepeater.Layout>
        <UniformGridLayout MinItemWidth="200" MinItemHeight="150" />
    </ItemsRepeater.Layout>
</ItemsRepeater>
```

#### When to use what

| Scenario | Approach |
|----------|----------|
| Reusable component adapts to its own size | Container Query |
| Desktop vs. mobile layout (static) | `OnFormFactor` |
| Flowing cards/tiles that wrap | `WrapPanel` or `UniformGridLayout` |
| Complex multi-property changes at breakpoints | Breakpoint ViewModel (observe window size, expose bool properties) |

**Key rules**:
- PREFER Container Queries over manual size-change event handling.
- ALWAYS use star sizing (`*`) and `Auto` in Grid definitions — avoid fixed pixel widths for content regions.
- `Visibility` enum replaced by `bool IsVisible`; for invisible-but-space-occupying use `Opacity="0"`.
- NO `VisualStateManager` — use pseudo-class selectors or Container Queries instead.

---

## Data Binding

### Binding Syntax

```xml
<!-- Basic binding -->
<TextBlock Text="{Binding PropertyName}" />

<!-- Binding with path -->
<TextBlock Text="{Binding Person.Name}" />

<!-- Binding modes -->
<TextBox Text="{Binding Name, Mode=TwoWay}" />
<!-- Modes: OneWay (default), TwoWay, OneTime, OneWayToSource -->

<!-- Binding to named element -->
<TextBlock x:Name="MyText" Text="Hello" />
<TextBox Text="{Binding #MyText.Text}" />

<!-- Binding to parent DataContext -->
<TextBlock Text="{Binding $parent[Window].DataContext.Title}" />

<!-- Binding with fallback -->
<TextBlock Text="{Binding Name, FallbackValue='Unknown'}" />

<!-- Binding with string format -->
<TextBlock Text="{Binding Price, StringFormat='${0:F2}'}" />

<!-- Binding with converter -->
<TextBlock Text="{Binding IsEnabled, Converter={StaticResource BoolToStringConverter}}" />
```

**Important**:
- `#ElementName.Property` is an Avalonia binding-path extension and should be used with Avalonia `Binding` / compiled bindings.
- Do **not** write `{ReflectionBinding #SomeElement.SomeCommand}`. `ReflectionBinding` treats the `#...` token as a plain path segment, so command/property lookup can fail at runtime.

### Compiled Bindings (Recommended)

Compiled bindings provide **compile-time safety** and **better performance**.

```xml
<!-- Enable compiled bindings globally in .csproj -->
<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>

<!-- Enable for specific view -->
<Window x:DataType="vm:MainViewModel"
        x:CompileBindings="True">
    
    <!-- Type-safe binding -->
    <TextBox Text="{Binding FirstName}" />
    <TextBox Text="{Binding LastName}" />
    
    <!-- Disable compile-time checking for a dynamic path only -->
    <Button Command="{ReflectionBinding DynamicCommandName}" />
</Window>

<!-- Or use CompiledBinding markup explicitly -->
<TextBox Text="{CompiledBinding FirstName}" />
```

**Best Practice**: Always use compiled bindings for type safety and performance. Use `ReflectionBinding` only for truly dynamic paths that cannot be typed, and never with `#ElementName` syntax.

### DataContext Type Inference (v11.3+)

```xml
<Window x:Name="MyWindow"
        x:DataType="vm:TestDataContext">
    
    <!-- Compiler infers DataContext type automatically -->
    <TextBlock Text="{Binding #MyWindow.DataContext.StringProperty}" />
    <TextBlock Text="{Binding $parent[Window].DataContext.StringProperty}" />
    
    <!-- No explicit type casting needed! -->
</Window>
```

### Multi-Binding

```xml
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="{}{0} {1}">
            <Binding Path="FirstName" />
            <Binding Path="LastName" />
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

### Element Binding

```xml
<!-- Bind to another element's property -->
<Slider x:Name="volumeSlider" Minimum="0" Maximum="100" Value="50" />
<TextBlock Text="{Binding #volumeSlider.Value}" />

<!-- Bind to parent control -->
<Border BorderThickness="{Binding $parent.IsMouseOver, 
                                  Converter={StaticResource BoolToThicknessConverter}}" />
```

---

## MVVM Pattern with CommunityToolkit.Mvvm

> ⚠️ **XerahS uses `CommunityToolkit.Mvvm`, NOT ReactiveUI.** Do not add or reference `ReactiveUI` or `Avalonia.ReactiveUI` packages.

### Install Package

```bash
dotnet add package CommunityToolkit.Mvvm
```

### ViewModel Base Pattern

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(1000); // Simulate save
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(FirstName);
}
```

**Source-generator notes**:
- `[ObservableProperty]` on a `private` field generates a public PascalCase property + `INotifyPropertyChanged` notification (`_firstName` → `FirstName`).
- `[RelayCommand]` generates `SaveAsyncCommand` (an `IAsyncRelayCommand`) automatically.
- The class **must** be `partial` for source generators to work.
- `[RelayCommand(CanExecute = nameof(...))]` wires can-execute automatically; call `SaveAsyncCommand.NotifyCanExecuteChanged()` when the condition changes.

### Architecture Layering

- **Views (AXAML)**: Visual composition only. No business logic in code-behind beyond `InitializeComponent()`.
- **ViewModels**: State, commands, and orchestration. UI-framework agnostic and unit-testable. Wire services via DI.
- **Services / Domain**: Business logic and data access — no references to Avalonia types.

### View Setup (code-behind)

```csharp
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = new MainViewModel(); // Or resolve via ViewLocator / DI
    }
}
```

### Command Binding in XAML

```xml
<!-- Generated command name: SaveAsyncCommand -->
<Button Content="Save" Command="{Binding SaveAsyncCommand}" />

<!-- With parameter -->
<Button Content="Delete"
        Command="{Binding DeleteCommand}"
        CommandParameter="{Binding SelectedItem}" />
```

### Property Changed Callbacks

```csharp
[ObservableProperty]
private string _name = string.Empty;

// Source-generated partial method — called automatically when Name changes
partial void OnNameChanged(string value)
{
    // React to change
}
```

### Manual Property Notifications (when source generators are unavailable)

```csharp
public partial class MyViewModel : ObservableObject
{
    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
```

---

## Styling & Theming

### Style Types

Avalonia has three styling mechanisms:

1. **Styles**: Similar to CSS, target controls by type or class
2. **Control Themes**: Complete visual templates (like WPF Styles)
3. **Container Queries**: Responsive styles based on container size

### Basic Styles

```xml
<Window.Styles>
    <!-- Style by Type -->
    <Style Selector="TextBlock">
        <Setter Property="Foreground" Value="White" />
        <Setter Property="FontSize" Value="14" />
    </Style>

    <!-- Style by Class -->
    <Style Selector="TextBlock.header">
        <Setter Property="FontSize" Value="24" />
        <Setter Property="FontWeight" Value="Bold" />
    </Style>

    <!-- Style by property -->
    <Style Selector="Button:pointerover">
        <Setter Property="Background" Value="LightBlue" />
    </Style>

    <!-- Nested selectors -->
    <Style Selector="StackPanel > Button">
        <Setter Property="Margin" Value="5" />
    </Style>
</Window.Styles>

<!-- Apply class -->
<TextBlock Classes="header" Text="Title" />
```

### Pseudo-classes

```xml
<Style Selector="Button:pointerover">         <!-- Mouse hover -->
<Style Selector="Button:pressed">             <!-- Mouse down -->
<Style Selector="Button:disabled">            <!-- Disabled state -->
<Style Selector="ListBoxItem:selected">       <!-- Selected item -->
<Style Selector="TextBox:focus">              <!-- Keyboard focus -->
<Style Selector="CheckBox:checked">           <!-- Checked state -->
<Style Selector="ToggleButton:unchecked">     <!-- Unchecked state -->
```

### Style Selectors

```xml
<!-- Descendant (any depth) -->
<Style Selector="StackPanel TextBlock">

<!-- Direct child -->
<Style Selector="StackPanel > TextBlock">

<!-- Multiple conditions (AND) -->
<Style Selector="Button.primary:pointerover">

<!-- Multiple selectors (OR) -->
<Style Selector="Button, ToggleButton">

<!-- Negation -->
<Style Selector="Button:not(.primary)">

<!-- Template parts -->
<Style Selector="Button /template/ ContentPresenter">
```

### Resources

```xml
<Window.Resources>
    <!-- Solid color brush -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="#007ACC" />
    
    <!-- Static resource -->
    <x:Double x:Key="StandardSpacing">10</x:Double>
    
    <!-- Gradient brush -->
    <LinearGradientBrush x:Key="GradientBrush" StartPoint="0%,0%" EndPoint="0%,100%">
        <GradientStop Color="#FF0000" Offset="0" />
        <GradientStop Color="#00FF00" Offset="1" />
    </LinearGradientBrush>
</Window.Resources>

<!-- Use resources -->
<Button Background="{StaticResource PrimaryBrush}" 
        Margin="{StaticResource StandardSpacing}" />

<!-- DynamicResource (updates when changed) -->
<Button Background="{DynamicResource PrimaryBrush}" />
```

### Themes

```xml
<!-- App.axaml -->
<Application.Styles>
    <!-- FluentTheme (Windows 11 style) -->
    <FluentTheme />
    
    <!-- Or Simple theme -->
    <SimpleTheme />
    
    <!-- Custom styles -->
    <StyleInclude Source="/Styles/CustomStyles.axaml" />
</Application.Styles>
```

---

## Dependency & Attached Properties

### StyledProperty (Dependency Property)

```csharp
public class MyControl : ContentControl
{
    // Define the property
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MyControl, string>(
            nameof(Title), 
            defaultValue: string.Empty);

    // CLR wrapper
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // React to property changes
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleProperty)
        {
            // Handle change
            var oldValue = (string?)change.OldValue;
            var newValue = (string?)change.NewValue;
        }
    }
}
```

### Attached Properties

```csharp
public class MyPanel : Panel
{
    // Define attached property
    public static readonly AttachedProperty<int> ColumnProperty =
        AvaloniaProperty.RegisterAttached<MyPanel, Control, int>(
            "Column",
            defaultValue: 0);

    // Getters/Setters
    public static int GetColumn(Control element)
        => element.GetValue(ColumnProperty);

    public static void SetColumn(Control element, int value)
        => element.SetValue(ColumnProperty, value);
}
```

```xml
<!-- Use attached property -->
<local:MyPanel>
    <Button local:MyPanel.Column="0" Content="First" />
    <Button local:MyPanel.Column="1" Content="Second" />
</local:MyPanel>
```

### Common Attached Properties

```xml
<!-- Grid -->
<Button Grid.Row="0" Grid.Column="1" Grid.RowSpan="2" Grid.ColumnSpan="3" />

<!-- DockPanel -->
<Menu DockPanel.Dock="Top" />

<!-- Canvas -->
<Rectangle Canvas.Left="50" Canvas.Top="100" />

<!-- ToolTip -->
<Button ToolTip.Tip="Click me!" />

<!-- ContextFlyout (Preferred with FluentAvalonia) -->
<Border>
    <Border.ContextFlyout>
        <MenuFlyout>
            <MenuItem Header="Copy" />
            <MenuItem Header="Paste" />
        </MenuFlyout>
    </Border.ContextFlyout>
</Border>
```

---

## ⚠️ XerahS-Specific: ContextMenu vs ContextFlyout

### Critical Issue with FluentAvalonia Theme

**Problem**: Standard `ContextMenu` controls do **not** render correctly with `FluentAvaloniaTheme`. They use legacy Popup windows which are not fully styled and may appear **unstyled or invisible**.

**Solution**: ✅ **Always use `ContextFlyout` with `MenuFlyout`** instead of `ContextMenu`.

```xml
<!-- ❌ INCORRECT: May be invisible with FluentAvalonia -->
<Border.ContextMenu>
    <ContextMenu>
        <MenuItem Header="Action" Command="{Binding MyCommand}"/>
    </ContextMenu>
</Border.ContextMenu>

<!-- ✅ CORRECT: Use ContextFlyout with MenuFlyout -->
<Border.ContextFlyout>
    <MenuFlyout>
        <MenuItem Header="Action" Command="{Binding MyCommand}"/>
    </MenuFlyout>
</Border.ContextFlyout>
```

### Binding in DataTemplates with Flyouts

**Problem**: When using `ContextFlyout` or `ContextMenu` inside a `DataTemplate`, bindings to the parent ViewModel fail because Popups/Flyouts exist in a **separate visual tree**, detached from the DataTemplate's hierarchy.

**Solution**: Use `$parent[UserControl].DataContext` to reach the main view's DataContext.

```xml
<DataTemplate x:DataType="local:MyItem">
    <Border>
        <Border.ContextFlyout>
            <MenuFlyout>
                <!-- ✅ Bind to parent UserControl's DataContext -->
                <MenuItem Header="Edit" 
                          Command="{Binding $parent[UserControl].DataContext.EditCommand}"
                          CommandParameter="{Binding}"/>
            </MenuFlyout>
        </Border.ContextFlyout>
        
        <TextBlock Text="{Binding Name}" />
    </Border>
</DataTemplate>
```

**Key Points**:
- Use `$parent[UserControl].DataContext` to access the View's ViewModel from within a flyout
- `CommandParameter="{Binding}"` passes the current data item (the DataTemplate's DataContext)
- For shared flyouts, define them in `UserControl.Resources` and reference via `{StaticResource}`

---

## Custom Controls

### Custom Control (Draws itself)

```csharp
public class CircleControl : Control
{
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<CircleControl, IBrush?>(nameof(Fill));

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var renderSize = Bounds.Size;
        var center = new Point(renderSize.Width / 2, renderSize.Height / 2);
        var radius = Math.Min(renderSize.Width, renderSize.Height) / 2;

        context.DrawEllipse(Fill, null, center, radius, radius);
    }
}
```

### Templated Control (Look-less)

```csharp
public class MyButton : TemplatedControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MyButton, string>(nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        // Find template parts
        var presenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
    }
}
```

### UserControl (Composite)

```xml
<!-- MyUserControl.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             x:Class="MyApp.Controls.MyUserControl">
    <StackPanel>
        <TextBlock Text="{Binding Title}" />
        <Button Content="Click Me" />
    </StackPanel>
</UserControl>
```

```csharp
// MyUserControl.axaml.cs
public partial class MyUserControl : UserControl
{
    public MyUserControl()
    {
        InitializeComponent();
    }
}
```

---

## Control Templates

### Define a ControlTheme

```xml
<ControlTheme x:Key="CustomButtonTheme" TargetType="Button">
    <Setter Property="Background" Value="Blue" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="Padding" Value="10,5" />
    <Setter Property="Template">
        <ControlTemplate>
            <Border Background="{TemplateBinding Background}"
                    BorderBrush="{TemplateBinding BorderBrush}"
                    BorderThickness="{TemplateBinding BorderThickness}"
                    CornerRadius="5">
                <ContentPresenter Name="PART_ContentPresenter"
                                  Content="{TemplateBinding Content}"
                                  Padding="{TemplateBinding Padding}"
                                  HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
                                  VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}" />
            </Border>
        </ControlTemplate>
    </Setter>
    
    <!-- Pseudo-class styles -->
    <Style Selector="^:pointerover /template/ Border">
        <Setter Property="Background" Value="LightBlue" />
    </Style>
    
    <Style Selector="^:pressed /template/ Border">
        <Setter Property="Background" Value="DarkBlue" />
    </Style>
</ControlTheme>

<!-- Apply theme -->
<Button Theme="{StaticResource CustomButtonTheme}" Content="Custom" />
```

### Template Parts

```csharp
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
public class MyTemplatedControl : TemplatedControl
{
    private ContentPresenter? _presenter;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _presenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
    }
}
```

---

## Resources & Converters

### Value Converters

```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? true : false; // Or specific logic
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool visible)
            return visible;
        return false;
    }
}
```

```xml
<Window.Resources>
    <local:BoolToVisibilityConverter x:Key="BoolToVisConverter" />
</Window.Resources>

<Border IsVisible="{Binding IsActive, Converter={StaticResource BoolToVisConverter}}" />
```

### Built-in Converters

```xml
<!-- Negation -->
<Button IsEnabled="{Binding !IsLoading}" />

<!-- Null check -->
<TextBlock IsVisible="{Binding MyObject, Converter={x:Static ObjectConverters.IsNotNull}}" />

<!-- String format -->
<TextBlock Text="{Binding Count, StringFormat='Items: {0}'}" />
```

---

## Events & Commands

### Event Handlers

```xml
<Button Click="OnButtonClick" Content="Click" />
```

```csharp
private void OnButtonClick(object? sender, RoutedEventArgs e)
{
    // Handle event
}
```

### Commands (MVVM)

```xml
<Button Command="{Binding SaveCommand}" 
        CommandParameter="{Binding CurrentItem}"
        Content="Save" />
```

```csharp
// Using CommunityToolkit.Mvvm
[RelayCommand]
private void Save(object? parameter)
{
    // Execute command
}

// Async command
[RelayCommand]
private async Task SaveAsync()
{
    await Task.Delay(100);
}

// Command with can-execute
[RelayCommand(CanExecute = nameof(CanDelete))]
private void Delete() { }

private bool CanDelete() => SelectedItem is not null;
```

### Routed Events

```csharp
public static readonly RoutedEvent<RoutedEventArgs> MyEvent =
    RoutedEvent.Register<MyControl, RoutedEventArgs>(
        nameof(MyEvent), 
        RoutingStrategies.Bubble);

public event EventHandler<RoutedEventArgs> MyEvent
{
    add => AddHandler(MyEvent, value);
    remove => RemoveHandler(MyEvent, value);
}

// Raise event
RaiseEvent(new RoutedEventArgs(MyEvent));
```

---

## Cross-Platform Patterns

### Platform Detection

```csharp
if (OperatingSystem.IsWindows())
{
    // Windows-specific code
}
else if (OperatingSystem.IsMacOS())
{
    // macOS-specific code
}
else if (OperatingSystem.IsLinux())
{
    // Linux-specific code
}
```

### Platform-Specific Resources

```xml
<Application.Styles>
    <StyleInclude Source="/Styles/Common.axaml" />
    
    <!-- Conditionally include styles -->
    <OnPlatform>
        <On Options="Windows">
            <StyleInclude Source="/Styles/Windows.axaml" />
        </On>
        <On Options="macOS">
            <StyleInclude Source="/Styles/macOS.axaml" />
        </On>
    </OnPlatform>
</Application.Styles>
```

### Design Principles

1. **Use .NET Standard**: Write business logic in .NET Standard libraries
2. **MVVM Pattern**: Separate UI from logic
3. **Avalonia Drawing**: Leverage Avalonia's drawn UI (not native controls)
4. **Platform Abstractions**: Use interfaces for platform-specific features
5. **Responsive Design**: Use container queries and adaptive layouts

---

## Performance & Best Practices

### Performance Tips

1. **Use `Panel` over `Grid`** when no rows/columns needed
2. **Enable compiled bindings** globally
3. **Use virtualization** for large lists (`ItemsRepeater`, `VirtualizingStackPanel`)
4. **Avoid deep nesting** of visual trees
5. **Use `RenderTransform`** instead of `Margin` for animations
6. **Recycle DataTemplates** with `ItemsRepeater`
7. **Minimize layout passes** by batching property changes

### Memory Management

`CommunityToolkit.Mvvm` source-generated properties raise `PropertyChanged` automatically — no manual subscription disposal is needed for `[ObservableProperty]` fields. When subscribing to external observables or events, implement `IDisposable`:

```csharp
public partial class MyViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;

    public MyViewModel(IMyService service)
    {
        _subscription = service.ValueStream.Subscribe(OnValue);
    }

    private void OnValue(string v) { }

    public void Dispose() => _subscription.Dispose();
}
```

### Null Safety

XerahS uses **strict nullable reference types**. Always:

```csharp
// Enable in .csproj
<Nullable>enable</Nullable>

// Handle nullability properly
public string? Title { get; set; }  // Nullable
public string Name { get; set; } = string.Empty;  // Non-nullable with default
```

### AOT and Trimming Awareness

For apps targeting mobile (iOS/Android), WebAssembly, or NativeAOT:

- **Prefer compiled bindings** (already the default) — they avoid reflection.
- Avoid `Activator.CreateInstance` for ViewModel/service resolution; use a DI container with AOT support (e.g. `Microsoft.Extensions.DependencyInjection` with source-generated registrations).
- Avoid `Type.GetType()`, `PropertyInfo.SetValue()`, or runtime reflection in hot paths.
- Use `[DynamicallyAccessedMembers]` annotations when reflection is unavoidable.
- Use `{ReflectionBinding}` sparingly — it is **not** trimming-safe, and do not combine it with Avalonia `#ElementName` paths.

---

## Developer Tools

> ⚠️ The legacy `Avalonia.Diagnostics` package is **deprecated**. NEVER use it.

Use the `migrate_diagnostics` MCP tool to set up or migrate Developer Tools in any project. It handles:
- Removing `Avalonia.Diagnostics`
- Installing `AvaloniaUI.DiagnosticsSupport`
- Configuring `Program.cs` and `App.axaml.cs`
- Replacing old API calls

```bash
# Install the global developer tools CLI
dotnet tool install --global AvaloniaUI.DeveloperTools
```

**Rule**: ALWAYS use `AvaloniaUI.DiagnosticsSupport` + the `AvaloniaUI.DeveloperTools` .NET global tool.

---

## Common Mistakes to Avoid

| # | Mistake | Correct Approach |
|---|---------|------------------|
| 1 | Using `.xaml` extension | Use `.axaml` |
| 2 | WPF namespaces | Use `https://github.com/avaloniaui` |
| 3 | `Style.Triggers` / `DataTrigger` / `EventTrigger` | Use pseudo-class selectors |
| 4 | `DependencyProperty` | Use `StyledProperty` or `DirectProperty` |
| 5 | `Style x:Key="..."` | Use style classes and selectors |
| 6 | `HierarchicalDataTemplate` | Use `TreeDataTemplate` |
| 7 | `pack://application:,,,/` URIs | Use `avares://AssemblyName/path` |
| 8 | `Visibility` enum | Use `bool IsVisible` (`Opacity="0"` for hidden-but-spaced) |
| 9 | Missing `x:DataType` | Always set it for compiled bindings |
| 10 | `{ReflectionBinding}` by default | Enable compiled bindings globally |
| 11 | `{ReflectionBinding #MyRoot.SomeCommand}` | Use `{Binding #MyRoot.SomeCommand}` (or compiled binding scope) |
| 12 | `Avalonia.Diagnostics` | Use `AvaloniaUI.DiagnosticsSupport` + `AvaloniaUI.DeveloperTools` |
| 13 | `ReactiveUI` / `Avalonia.ReactiveUI` | Use `CommunityToolkit.Mvvm` |
| 14 | Manual `ContentControl.Content` page swapping | Use `NavigationPage` / `TabbedPage` / `DrawerPage` |
| 15 | `VisualStateManager` | Use pseudo-class selectors or Container Queries |
| 16 | `LayoutTransform` | Wrap in `LayoutTransformControl` |
| 17 | `Dispatcher.Invoke()` | Use `Dispatcher.UIThread.InvokeAsync()` |
| 18 | `ContextMenu` with FluentAvalonia | Use `ContextFlyout` + `MenuFlyout` |
| 19 | `ScrollViewer Padding="N"` around a `StackPanel` | Move padding to inner element: `<StackPanel Margin="N">` — `ScrollViewer.Padding` shrinks the viewport only, not the scroll extent; bottow content stays permanently unreachable |
| 20 | `SplitView` as a two-column non-pane shell | Use `Grid ColumnDefinitions="auto,*"` — `SplitView` inherits `ContentControl` whose default `VerticalContentAlignment=Top` passes `∞` height to children, breaking any nested `ScrollViewer` |
| 21 | `TransitioningContentControl` as a page host containing a `ScrollViewer` | Use plain `ContentControl HorizontalContentAlignment="Stretch" VerticalContentAlignment="Stretch"` — `TransitioningContentControl`'s animation panel passes `∞` height during measure |
| 22 | `TabControl` at default `VerticalContentAlignment` wrapping scrollable tabs | Set `VerticalContentAlignment="Stretch"` on the `TabControl` — the inner `ContentPresenter` templates to `{TemplateBinding VerticalContentAlignment}` and defaults to `Top`, passing `∞` height to tab bodies |

---

## Common Patterns in XerahS

### StyledProperty Pattern

```csharp
public static readonly StyledProperty<object?> SelectedObjectProperty =
    AvaloniaProperty.Register<PropertyGrid, object?>(nameof(SelectedObject));

public object? SelectedObject
{
    get => GetValue(SelectedObjectProperty);
    set => SetValue(SelectedObjectProperty, value);
}
```

### Attached Property Pattern (Auditing)

```csharp
public static readonly AttachedProperty<bool> IsUnwiredProperty =
    AvaloniaProperty.RegisterAttached<UiAudit, Control, bool>("IsUnwired");

public static bool GetIsUnwired(Control control)
    => control.GetValue(IsUnwiredProperty);

public static void SetIsUnwired(Control control, bool value)
    => control.SetValue(IsUnwiredProperty, value);
```

### Window Structure

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:XerahS.ViewModels"
        x:Class="XerahS.UI.Views.MainWindow"
        x:DataType="vm:MainViewModel"
        x:CompileBindings="True"
        Title="{Binding Title}"
        Width="1000" Height="700">
    
    <Window.Styles>
        <!-- Local styles -->
    </Window.Styles>
    
    <DockPanel>
        <!-- Layout content -->
    </DockPanel>
</Window>
```

---

## Quick Reference Tables

### Alignment Values

| Property | Values | Default |
|----------|--------|---------|
| `HorizontalAlignment` | `Left`, `Center`, `Right`, `Stretch` | `Stretch` |
| `VerticalAlignment` | `Top`, `Center`, `Bottom`, `Stretch` | `Stretch` |

### Binding Modes

| Mode | Direction | Updates |
|------|-----------|---------|
| `OneWay` | Source → Target | Source changes |
| `TwoWay` | Source ↔ Target | Both changes |
| `OneTime` | Source → Target | Once at init |
| `OneWayToSource` | Source ← Target | Target changes |

### Grid Sizing

| Type | Syntax | Behavior |
|------|--------|----------|
| **Auto** | `Auto` | Size to content |
| **Pixel** | `100` | Fixed size |
| **Star** | `*` or `2*` | Proportional fill |

---

## Additional Resources

- **Official Docs**: https://docs.avaloniaui.net/
- **GitHub**: https://github.com/AvaloniaUI/Avalonia
- **Samples**: https://github.com/AvaloniaUI/Avalonia/tree/master/samples
- **CommunityToolkit.Mvvm**: https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/
- **FluentAvalonia**: https://github.com/amwx/FluentAvalonia
- **Community**: https://avaloniaui.community/

---

## Checklist for New Controls/Views

- [ ] Use `.axaml` file extension
- [ ] Set `x:Class` attribute
- [ ] Set `x:DataType` for compiled bindings
- [ ] Set `x:CompileBindings="True"` (or enable globally in `.csproj`)
- [ ] Define proper namespaces
- [ ] Use `StyledProperty` for styleable custom properties; `DirectProperty` for non-styleable/perf-critical ones
- [ ] Follow nullable reference type rules
- [ ] Use `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`) for MVVM — **NOT ReactiveUI**
- [ ] Mark ViewModel classes `partial` for source generators
- [ ] Apply consistent styling/theming
- [ ] ⚠️ **Use `ContextFlyout` + `MenuFlyout`, NOT `ContextMenu`** (FluentAvalonia compatibility)
- [ ] Use `$parent[UserControl].DataContext` for flyout bindings in DataTemplates
- [ ] Use Avalonia `Binding` for `#ElementName` paths; never `{ReflectionBinding #...}`
- [ ] Use `NavigationPage`/`TabbedPage`/`DrawerPage` for multi-page apps — not manual `ContentControl` swapping
- [ ] AOT/trimming: prefer compiled bindings; avoid runtime reflection
- [ ] Use `AvaloniaUI.DiagnosticsSupport` — never `Avalonia.Diagnostics`
- [ ] Handle accessibility (tab order, accessible names)
- [ ] Test on all target platforms

---

**Last Updated**: March 17, 2026  
**Version**: 1.3.1  
**Maintained by**: XerahS Development Team
