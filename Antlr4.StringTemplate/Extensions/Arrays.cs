namespace Antlr4.StringTemplate.Extensions;

using System;

internal static class Arrays {

    public static TOutput[] ConvertAll<TInput, TOutput>(TInput[] array, Func<TInput, TOutput> transform) {
        if (array == null) {
            throw new ArgumentNullException(nameof(array));
        }
        if (transform == null) {
            throw new ArgumentNullException(nameof(transform));
        }
        var result = new TOutput[array.Length];
        for (var i = 0; i < array.Length; i++) {
            result[i] = transform(array[i]);
        }
        return result;
    }

}
