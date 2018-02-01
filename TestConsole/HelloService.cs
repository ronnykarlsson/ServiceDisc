namespace TestConsole
{
    public class HelloService : IHelloService
    {
        private readonly string _name;

        public HelloService(string name)
        {
            _name = name;
        }

        public string Hello(string name)
        {
            return $"Hello {name}, this is {_name}";
        }

        public int Add(int num1, int num2)
        {
            return num1 + num2;
        }

        public ComplexHello Increase(ComplexHello complexHello)
        {
            complexHello.Counter++;
            return complexHello;
        }
    }

    public interface IHelloService
    {
        string Hello(string name);
        int Add(int num1, int num2);
        ComplexHello Increase(ComplexHello complexHello);
    }

    public class ComplexHello
    {
        public int Counter { get; set; }
    }
}