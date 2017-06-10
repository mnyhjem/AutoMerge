using System;

namespace AutoMerge.Prism
{
    public class SimpleDeletegateCommand : SimpleCommand
    {

        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        public SimpleDeletegateCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public SimpleDeletegateCommand(Action<object> execute,
            Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }



        public override bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute(parameter);
        }

        public override void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
