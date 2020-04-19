/*
Vectoray; Home-brew 3D C# game engine.
Copyright (C) 2020 LukeZaz

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

#region Explanation: Why not nullables?
// While that'd work fine for value types, it wouldn't for reference types. The reasons for this are threefold:
// First and least importantly, C# 8.0 requires nullabe reference types first be enabled by way of
// either a preprocessor directive or setting the <Nullable> tag in the .csproj file. This is annoying and I'm
// not sure why it's not enabled by default.

// Second and more importantly, they generate warnings instead of errors. Attempting to use a nullable type
// should prevent successful compilation so that these issues do not leak through to runtime.

// Lastly, C# nullables do not allow you to pack in error information, so Result<T, E> would've been necessary anyway.
// Exceptions just aren't as good by comparison, and they're only used in this program
// for situations in which their *not* being caught is the whole point; i.e., "this needs to crash now because
// something went very seriously wrong"
#endregion

using System;

namespace Vectoray
{
    // TODO: Like all the other 'unsafe' methods of unwrapping outcomes, this should be moved elsewhere so it
    // requires a separate 'using', so as to discourage use.
    // TODO: Unit testing.
    /// <summary>
    /// An exception that is thrown when an `Opt&lt;T&gt;` is unwrapped, but its value is `null` or `None`.
    /// </summary>
    public class EmptyUnwrapException : Exception
    {
        public EmptyUnwrapException() : base() { }
        public EmptyUnwrapException(string message) : base(message) { }
        public EmptyUnwrapException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// A helper class containing various extensions to make working with `Opt&lt;T&gt;`
    /// and `Result&lt;T, E&gt;` types easier.
    /// </summary>
    public static class Outcomes
    {
        /// <summary>
        /// Create a new `Valid` which wraps this value and associates an error type with it.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <typeparam name="T">The value's type.</typeparam>
        /// <typeparam name="E">The error type to associate.</typeparam>
        /// <returns>A new `Valid&lt;T, E&gt; that contains the given value.</returns>
        public static Valid<T, E> Valid<T, E>(this T value) where E : Exception => new Valid<T, E>(value);

        /// <summary>
        /// Create a new `Invalid` which wraps this error and associates a success type with it.
        /// </summary>
        /// <param name="error">The error to wrap.</param>
        /// <typeparam name="T">The success value type to associate.</typeparam>
        /// <typeparam name="E">The error's type.</typeparam>
        /// <returns>A new `Invalid&lt;T, E&gt; that contains the given error.</returns>
        public static Invalid<T, E> Invalid<T, E>(this E error) where E : Exception => new Invalid<T, E>(error);

        /// <summary>
        /// Create a new `Some` which wraps this value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <typeparam name="T">The value's type.</typeparam>
        /// <returns>A new `Some&lt;T&gt;` that contains the given value.</returns>
        public static Some<T> Some<T>(this T value) => new Some<T>(value);

        /// <summary>
        /// Create a new `None` using this value's type.
        /// </summary>
        /// <typeparam name="T">The value's type.</typeparam>
        /// <returns>A new `None&lt;T&gt;`.</returns>
        public static None<T> None<T>(this T _) => new None<T>();
    }

    #region Result types

    /// <summary>
    /// Used to wrap a value and an error type for containing information about
    /// multiple types of failure state, such that the value in question cannot be
    /// used without also handling the failure states in some way.
    /// 
    /// While other methods of extracting the inner values are available (e.g. `UnwrapOr(defaultValue)`),
    /// the recommended method is to use pattern matching, e.g.:
    /// ```
    /// Result&lt;int, string&gt; example = new Valid&lt;int, string&gt;(5);
    /// if (example is Valid&lt;int, string&gt;(int inner))
    /// Console.WriteLine(inner); // Prints '5'.
    /// ```
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="E">The error type which extends `System.Exception`.</typeparam>
    #region Explanation for abstract class
    // While Some<T> and None<T> are both perfect fits for being structs, and thus Opt<T> would be fine as an interface,
    // there are two issues that prevent this from being ideal:

    // A) A struct cannot have a custom parameterless constructor. This means the default parameterless
    //    constructor cannot be made inaccessible, making it all too easy to create an invalid Some<T> with it.

    //    It's true you can do this regardless using by using arrays, but the goal is not to make it
    //    impossible - only hard enough that doing so makes you think, "Should I *really* be doing this?"

    // B) Interfaces can only have public accessibility. Due to this, it's not possible to prevent classes
    //    from outside this assembly from inheriting it. It should be a guarantee that anything that
    //    returns Opt<T> is either Some<T> or None<T>; allowing other classes to extend Opt<T> ruins
    //    this assumption.

    // A regular class that simply uses pattern matching to figure out what it is and return accordingly would
    // also work, however I'm unaware of any way to customize the XML docs for the subclasses in this scenario.
    // Additionally, seeing as the XML docs comprise probably about 85% of the contents of this file, compacting the
    // code into the base class would save nothing unless it first eliminated a huge amount of said documentation.
    #endregion
    public abstract class Result<T, E> where E : Exception
    {
        #region Explanation for access modifier
        // Private protected limits access to the containing class or any that derives from it
        // within the same assembly. Since classes cannot inherit from a class whose constructors it does
        // not have access to, this means that inheriting Opt<T> is limited *and* that users of Opt derived classes
        // are forced to use their constructors only.

        // The reason for this is that Opt should only be inherited by two classes: Some and None,
        // so as to ensure it and its derivatives form a simple and predictable structure.
        // If a function returns Opt<T>, you should be able to assume it's either Some or None;
        // an unexpected third class would only introduce confusion.
        #endregion
        private protected Result() { }

        /// <summary>
        /// Attempt to unwrap this result and retrieve the value inside, or return `defaultValue`
        /// if this result is an `Invalid`.
        /// 
        /// **Examples:**
        /// ```
        /// Result&lt;int, string&gt; errorResult = new Error&lt;int, string&gt;("It's dead, Jim.");
        /// int val = errorResult.UnwrapOr(5);
        /// Console.WriteLine(val); // Prints '5'.
        /// 
        /// Result&lt;int, string&gt; validResult = new Valid&lt;int, string&gt;(10);
        /// int val = validResult.UnwrapOr(5);
        /// Console.WriteLine(val); // Prints '10'.
        /// ```
        /// </summary>
        /// <param name="defaultValue">The value to default to if this result is an `Invalid`.</param>
        /// <returns>The inner value of this result, or `defaultValue` if this result is an `Invalid`.</returns>
        public abstract T UnwrapOr(T defaultValue);
    }

    /// <summary>
    /// A container deriving `Result&lt;T, E&gt;`, used to represent a successful result.
    /// 
    /// While other methods of extracting the inner value are available (e.g. `UnwrapOr(defaultValue)`),
    /// the recommended method is to use pattern matching, e.g.:
    /// ```
    /// Result&lt;int, string&gt; example = new Valid&lt;int, string&gt;(5);
    /// if (example is Valid&lt;int, string&gt;(int inner))
    /// Console.WriteLine(inner); // Prints '5'.
    /// ```
    /// </summary>
    /// <typeparam name="T">The type of the inner value of this `Valid`.</typeparam>
    /// <typeparam name="E">The unused error type of the result this `Valid` derives.</typeparam>
    public sealed class Valid<T, E> : Result<T, E> where E : Exception
    {
        private readonly T value;
        private T Value
        {
            get
            {
                // An exception is thrown here because if you create an instance of Valid
                // with a null inner value (despite it disallowing this in its constructor),
                // then something's wrong.
                if (value == null)
                    throw new NullReferenceException(
                        $"Inner value of {typeof(Valid<T, E>)} was retrieved, but was found to be null.");
                else return value;
            }
        }

        /// <summary>
        /// Create a new `Valid` to wrap `value`.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <exception cref="NullReferenceException">Thrown if `value` is null.</exception>
        public Valid(T value)
        {
            if (value == null)
                throw new ArgumentNullException(
                    $"Cannot create an instance of {typeof(Valid<T, E>)} with a null inner value.");
            this.value = value;
        }

        /// <summary>
        /// Unwrap this `Valid` and retrieve the value inside. While other results use `defaultValue`
        /// as a backup value, null values are not allowed for instances of `Valid`; hence, `defaultValue`
        /// will never be returned.
        /// 
        /// For obvious reasons, you probably don't want to use this function.
        /// Instead, use pattern matching with `switch` or `is`.
        /// </summary>
        /// <param name="defaultValue">The value that would be defaulted to, were this an instance of `Error` instead.
        /// Never used.</param>
        /// <returns>The value encapsulated by this `Valid`.</returns>
        /// <exception cref="NullReferenceException">
        /// Can be thrown in the extraordinary event an instance of `Valid` is somehow created with a null inner value.
        /// This should never happen and represents a serious error, so this exception is used instead of `defaultValue`.
        /// </exception>
        public override T UnwrapOr(T defaultValue) => Value;

        /// <summary>
        /// Deconstruct this `Valid` and retrieve the inner value.
        /// </summary>
        /// <param name="value">The variable to fill with the inner value of this `Valid`.</param>
        /// <exception cref="NullReferenceException">
        /// Can be thrown in the extraordinary event an instance of `Valid` is somehow created with a null inner value.
        /// This should never happen and represents a serious error, so this exception is used instead of remaining
        /// silent.
        /// </exception>
        public void Deconstruct(out T value) => value = Value;
    }

    /// <summary>
    /// A container deriving `Result&lt;T, E&gt;`, used to represent an unsuccessful result.
    /// 
    /// While other methods of extracting the error value are available (e.g. `UnwrapOr(defaultValue)`),
    /// the recommended method is to use pattern matching, e.g.:
    /// ```
    /// Result&lt;int, string&gt; example = new Invalid&lt;int, string&gt;("It's dead, Jim.");
    /// if (example is Invalid&lt;int, string&gt;(string error))
    /// Console.WriteLine(error); // Prints "It's dead, Jim."
    /// ```
    /// </summary>
    /// <typeparam name="T">The unused success type of the result this `Valid` derives.</typeparam>
    /// <typeparam name="E">The type of the error value of this `Invalid`.</typeparam>
    public sealed class Invalid<T, E> : Result<T, E> where E : Exception
    {
        private readonly E errorValue;
        public E ErrorValue => errorValue ?? throw new NullReferenceException(
                $"Inner value of {typeof(Invalid<T, E>)} was retrieved, but was found to be null.");

        /// <summary>
        /// Create a new `Invalid` to wrap `errorValue`.
        /// </summary>
        /// <param name="errorValue">The error to wrap.</param>
        /// <exception cref="NullReferenceException">Thrown if `errorValue` is null.</exception>
        public Invalid(E errorValue) =>
            this.errorValue = errorValue ?? throw new ArgumentNullException(
                $"Cannot create an instance of {typeof(Invalid<T, E>)} with a null inner value.");

        /// <summary>
        /// Attempt to unwrap an inner value from this `Invalid`, using `defaultValue` as a backup value. As this
        /// type represents an error, this method will **always** return `defaultVal`.
        /// 
        /// For obvious reasons, you probably don't want to use this method.
        /// </summary>
        /// <param name="defaultValue">The value to return, as this class cannot be successfully unwrapped.</param>
        /// <returns>Always returns `defaultValue`.</returns>
        public override T UnwrapOr(T defaultValue) => defaultValue;

        /// <summary>
        /// Deconstruct this `Invalid` and retrieve the inner error value.
        /// </summary>
        /// <param name="errorValue">The variable to fill with the inner error value of this `Invalid`.</param>
        /// <exception cref="NullReferenceException">
        /// Can be thrown in the extraordinary event an instance of `Invalid` is somehow created with a null inner value.
        /// This should never happen and represents a serious error, so this exception is thrown instead of remaining
        /// silent.
        /// </exception>
        public void Deconstruct(out E errorValue) => errorValue = ErrorValue;
    }

    #endregion

    #region Option types

    // TODO: Remove Unwrap. Place it in a separate namespace?
    // TODO: Add static extensions class of some kind. Create an ext method to convert T to Some/None<T>.
    /// <summary>
    /// Used to wrap a value that may be null such that it cannot be used until properly checked,
    /// thereby avoiding NullReference exceptions.
    /// 
    /// Various methods exist to retrieve the inner value, including `Unwrap` and `UnwrapOr(T defaultVal)`,
    /// however the recommended method is to use pattern matching, e.g.:
    /// ```
    /// Opt&lt;int&gt; example = new Some&lt;int&gt;(5);
    /// if (example is Some&lt;int&gt;(int inner))
    /// Console.WriteLine(inner); // Prints '5'.
    /// ```
    /// </summary>
    /// <typeparam name="T">The wrapped type of this Option.</typeparam>
    // This is an abstract class for the same reasons as Result<T, E>. See the comment block on it for details.
    public abstract class Opt<T>
    {
        // Likewise, the reasons for the access modifier are the same as Result<T, E> as well.
        private protected Opt() { }

        /// <summary>
        /// Attempt to unwrap this option and retrieve the value inside.
        /// 
        /// It's not recommended to use this method
        /// unless a value of `None` constitutes an extraordinary error; instead, use pattern
        /// matching via `is`. See the documentation for `Opt&lt;T&gt;` for an example.
        /// 
        /// Will throw an `EmptyUnwrapException` if this option is either `None` or has an inner value of `null`.
        /// </summary>
        /// <returns>
        /// The value encapsulated by this option, provided this option represents an instance of `Some&lt;T&gt;`.
        /// </returns>
        /// <exception cref="EmptyUnwrapException">
        /// Thrown if this option is either a `None` class or has an inner value of `null`.
        /// </exception>
        public abstract T Unwrap();

        /// <summary>
        /// Attempt to unwrap this option and retrieve the value inside, or return `defaultVal`
        /// if this option is either `None` or `null`.
        /// 
        /// **Example:**
        /// ```
        /// Opt&lt;int&gt; empty = new None&lt;int&gt;();
        /// int val = empty.UnwrapOr(5);
        /// Console.WriteLine(val); // Prints '5'.
        /// ```
        /// </summary>
        /// <param name="defaultValue">The value to default to if this option is either `None` or `null`.</param>
        /// <returns>The inner value of this option, or `defaultVal` if this option is either `None` or `null`.</returns>
        public abstract T UnwrapOr(T defaultValue);

        /// <summary>
        /// Attempt to unwrap this option and retrieve the value inside. If `None` or `null` is encountered,
        /// this will use `message` to throw an `EmptyUnwrapException`.
        /// 
        /// It's not recommended to use this method
        /// unless a value of `None` constitutes an extraordinary error; instead, use pattern
        /// matching via `is`. See the documentation for `Opt&lt;T&gt;` for an example.
        /// </summary>
        /// <param name="message">The message to pass to any `EmptyUnwrapException`s thrown by this method.</param>
        /// <returns>
        /// The value encapsulated by this option, provided this option represents an instance of `Some&lt;T&gt;`.
        /// </returns>
        /// <exception cref="EmptyUnwrapException">
        /// Thrown if this option is either a `None` class or has an inner value of `null`.
        /// </exception>
        public abstract T Expect(string message);

        /// <summary>
        /// Map this `Opt&lt;T&gt;` to `Opt&lt;R&gt;` by using the provided function
        /// to convert the inner value.
        /// </summary>
        /// <param name="mapFunction">The function to use to map the inner value.</param>
        /// <typeparam name="R">The new `Opt` type to return.</typeparam>
        /// <returns>The converted `Opt&lt;R&gt;` value.</returns>
        public abstract Opt<R> Map<R>(Func<T, R> mapFunction);
    }

    /// <summary>
    /// A container deriving `Opt&lt;T&gt;`, used to represent an option that has a usable inner value.
    /// 
    /// Various methods exist to retrieve the inner value, including `Unwrap` and `UnwrapOr(T defaultVal)`,
    /// however the recommended method is to use pattern matching, e.g.:
    /// ```
    /// Opt&lt;int&gt; example = new Some&lt;int&gt;(5);
    /// if (example is Some&lt;int&gt;(int inner))
    /// Console.WriteLine(inner); // Prints '5'.
    /// ```
    /// </summary>
    /// <typeparam name="T">The type of the inner value of this `Some`.</typeparam>
    public sealed class Some<T> : Opt<T>
    {
        private readonly T value;

        /// <summary>
        /// Create a new `Some` to wrap `value`.
        /// </summary>
        /// <param name="value">The value to wrap in this `Some`. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if `value` is null.</exception>
        public Some(T value)
        {
            if (value == null) throw new ArgumentNullException("Cannot create a Some<T> with a null value.");
            else this.value = value;
        }

        public static implicit operator Some<T>(T value) => new Some<T>(value);

        /// <summary>
        /// Unwrap this `Some` and retrieve the value inside.
        /// </summary>
        /// <returns>The value encapsulated by this `Some`.</returns>
        /// <exception cref="EmptyUnwrapException">
        /// Can be thrown in the extraordinary event an instance of `Some` is somehow created with a null inner value.
        /// </exception>
        public override T Unwrap() => Expect("Attempted to call Opt<T>.Unwrap on a Some<T> with a value of null. (...how?)");

        /// <summary>
        /// Unwrap this `Some` and retrieve the value inside. While other options use `defaultVal`
        /// as a backup value, null values are not allowed for instances of `Some`; hence, `defaultVal`
        /// will never be returned.
        /// 
        /// For obvious reasons, you probably don't want to use this function.
        /// Instead, use either pattern matching or `Unwrap()`.
        /// </summary>
        /// <param name="defaultValue">The value that would be defaulted to, were this an instance of `None` instead.
        /// Never used.</param>
        /// <returns>The value encapsulated by this `Some`.</returns>
        /// <exception cref="EmptyUnwrapException">
        /// Can be thrown in the extraordinary event an instance of `Some` is somehow created with a null inner value.
        /// This should never happen and represents a serious error, so this exception is used instead of `defaultVal`.
        /// </exception>
        public override T UnwrapOr(T defaultValue) =>
            Expect("Attempted to call Opt<T>.UnwrapOr on a Some<T> with a value of null. (...how?)");

        /// <summary>
        /// Unwrap this `Some` and retrieve the value inside.
        /// In the extraordinary event this fails, this will use `message` to throw a custom `EmptyUnwrapException`.
        /// </summary>
        /// <returns>The value encapsulated by this `Some`.</returns>
        /// <exception cref="EmptyUnwrapException">
        /// Can be thrown in the extraordinary event an instance of `Some` is somehow created with a null inner value.
        /// If this happens, `message` will be used to create the exception.
        /// </exception>
        public override T Expect(string message)
        {
            // This value should really, *really* never be null, but it's not impossible, so check anyway.
            if (value == null) throw new EmptyUnwrapException(message);
            else return value;
        }

        /// <summary>
        /// Map this `Some&lt;T&gt;` to `Some&lt;R&gt;` by using the provided function
        /// to convert the inner value.
        /// </summary>
        /// <param name="mapFunction">The function to use to map the inner value.</param>
        /// <typeparam name="R">The new `Some` type to return.</typeparam>
        /// <returns>The converted `Some&lt;R&gt;` value.</returns>
        public override Opt<R> Map<R>(Func<T, R> mapFunction) => new Some<R>(mapFunction(value));

        /// <summary>
        /// Deconstruct this `Some` and retrieve the inner value.
        /// </summary>
        /// <param name="value">The variable to fill with the inner value of this `Some`.</param>
        public void Deconstruct(out T value) => value = Unwrap();
    }

    /// <summary>
    /// A container deriving `Opt&lt;T&gt;`, used to represent an option that has no inner value.
    /// 
    /// Various methods exist to check for this type, including `Unwrap` and `UnwrapOr(T defaultVal)`,
    /// however the recommended method is to use pattern matching, e.g.:
    /// ```
    /// Opt&lt;int&gt; example = new None&lt;int&gt;();
    /// if (example is None&lt;int&gt;)
    /// Console.WriteLine("'Example' was an empty option.");
    /// ```
    /// </summary>
    /// <typeparam name="T">The type of what would be the inner value of this `None`, were it instead a `Some`.</typeparam>
    public sealed class None<T> : Opt<T>
    {
        /// <summary>
        /// Create a new `None` value.
        /// </summary>
        public None() { }

        /// <summary>
        /// Attempt to unwrap an inner value from this `None`. As this does not have
        /// an inner value, this **will** throw an EmptyUnwrapException.
        /// 
        /// For obvious reasons, you probably don't want to use this method.
        /// </summary>
        /// <returns>Nothing; this method is guaranteed to throw an EmptyUnwrapException.</returns>
        /// <exception cref="EmptyUnwrapException">Always thrown.</exception>
        public override T Unwrap() => Expect("Attempted to call Opt<T>.Unwrap on a None value.");

        /// <summary>
        /// Attempt to unwrap an inner value from this `None`, using `defaultVal` as a backup value. As this does
        /// not have an inner value, this will **always** return `defaultVal`.
        /// 
        /// For obvious reasons, you probably don't want to use this method.
        /// </summary>
        /// <param name="defaultValue">The value to return, as this class cannot be successfully unwrapped.</param>
        /// <returns>Always returns `defaultVal`.</returns>
        public override T UnwrapOr(T defaultValue) => defaultValue;

        /// <summary>
        /// Attempt to unwrap an inner value from this `None`. As this does not have
        /// an inner value, this **will** throw an EmptyUnwrapException using `message` as its message.
        /// 
        /// For obvious reasons, you probably don't want to use this method.
        /// </summary>
        /// <param name="message">The message to pass to the `EmptyUnwrapException` thrown by this method.</param>
        /// <returns>Nothing; this method is guaranteed to throw an EmptyUnwrapException.</returns>
        /// <exception cref="EmptyUnwrapException">Always thrown.</exception>
        public override T Expect(string message) =>
            throw new EmptyUnwrapException(message);

        /// <summary>
        /// Map this `None&lt;T&gt;` to `None&lt;R&gt;` by using the provided function
        /// to convert the inner value.
        /// </summary>
        /// <param name="mapFunction">The function to use to map the inner value.</param>
        /// <typeparam name="R">The new `None` type to return.</typeparam>
        /// <returns>The converted `None&lt;R&gt;` value.</returns>
        public override Opt<R> Map<R>(Func<T, R> mapFunction) => new None<R>();
    }

    #endregion

    // Why yes, I did program in Rust for a while! Why do you ask?
}