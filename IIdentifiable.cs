namespace Tiler.Editor;

public interface IIdentifiable<out TId>
{
    TId ID { get; }
}