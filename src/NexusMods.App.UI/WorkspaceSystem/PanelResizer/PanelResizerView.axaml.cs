using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public partial class PanelResizerView : ReactiveUserControl<IPanelResizerViewModel>
{
    private bool _isPressed;
    private Point _startPoint;

    public PanelResizerView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(PopulateFromViewModel)
                .Subscribe()
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.ActualPosition)
                .SubscribeWithErrorLogging(point =>
                {
                    var x = point.X - Bounds.Width / 2;
                    var y = point.Y - Bounds.Height / 2;

                    Canvas.SetLeft(this, x);
                    Canvas.SetTop(this, y);
                })
                .DisposeWith(disposables);

            // hover
            Observable.FromEventPattern<PointerEventArgs>(
                addHandler: handler => PointerEntered += handler,
                removeHandler: handler => PointerEntered -= handler)
                .Do(_ =>
                {
                    if (ViewModel is null) return;

                    SetCursor(ViewModel.IsHorizontal
                        ? StandardCursorType.SizeNorthSouth
                        : StandardCursorType.SizeWestEast
                    );
                })
                .Subscribe()
                .DisposeWith(disposables);

            // pressed
            Observable.FromEventPattern<PointerPressedEventArgs>(
                    addHandler: handler => PointerPressed += handler,
                    removeHandler: handler => PointerPressed -= handler)
                .Do(eventPattern =>
                {
                    _isPressed = true;
                    _startPoint = eventPattern.EventArgs.GetPosition(Parent! as Control);
                })
                .Finally(() => _isPressed = false)
                .Subscribe()
                .DisposeWith(disposables);

            // released
            Observable.FromEventPattern<PointerReleasedEventArgs>(
                    addHandler: handler => PointerReleased += handler,
                    removeHandler: handler => PointerReleased -= handler)
                .Do(_ =>
                {
                    _isPressed = false;
                    _startPoint = new Point(0, 0);

                    SetCursor(StandardCursorType.Arrow);
                })
                .Subscribe()
                .DisposeWith(disposables);

            // moved
            Observable.FromEventPattern<PointerEventArgs>(
                    addHandler: handler => PointerMoved += handler,
                    removeHandler: handler => PointerMoved -= handler)
                .Where(_ => _isPressed && _startPoint != new Point(0, 0))
                .Select(eventPattern =>
                {
                    if (ViewModel is null) return new Point(0, 0);

                    SetCursor(ViewModel.IsHorizontal
                        ? StandardCursorType.SizeNorthSouth
                        : StandardCursorType.SizeWestEast
                    );

                    var parent = (Parent as Control)!;
                    var currentPos = eventPattern.EventArgs.GetPosition(parent);

                    var newPosition = new Point(
                        ViewModel.IsHorizontal ? _startPoint.X : currentPos.X,
                        ViewModel.IsHorizontal ? currentPos.Y: _startPoint.Y
                    );

                    return newPosition;
                })
                .InvokeCommand(this, view => view.ViewModel!.DragCommand)
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(IPanelResizerViewModel viewModel)
    {
        Icon.Classes.Add(viewModel.IsHorizontal ? "DragHorizontal" : "DragVertical");
    }

    private void SetCursor(StandardCursorType standardCursorType)
    {
        // TODO: doesn't work?
        Cursor = new Cursor(standardCursorType);

        // var topLevel = TopLevel.GetTopLevel(this);
        // Console.WriteLine(topLevel);
        //
        // if (topLevel is not null)
        // {
        //     topLevel.Cursor = new Cursor(standardCursorType);
        // }
    }
}

