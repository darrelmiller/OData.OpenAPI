﻿//---------------------------------------------------------------------
// <copyright file="OpenApiWriterBase.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.OData.OpenAPI.Properties;

namespace Microsoft.OData.OpenAPI
{
    /// <summary>
    /// Base class for Open API writer.
    /// </summary>
    internal abstract class OpenApiWriterBase : IOpenApiWriter
    {
        /// <summary>
        /// The indentation string to prepand to each line for each indentation level.
        /// </summary>
        private const string IndentationString = "  ";

        /// <summary>
        /// Number which specifies the level of indentation. Starts with 0 which means no indentation.
        /// </summary>
        private int indentLevel;

        /// <summary>
        /// Scope of the Open API element - object, array, property.
        /// </summary>
        protected readonly Stack<Scope> scopes;

        /// <summary>
        /// Number which specifies the level of indentation. Starts with 0 which means no indentation.
        /// </summary>
        private OpenApiWriterSettings settings;

        protected virtual int IndentShift { get { return 0; } }

        protected TextWriter Writer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiWriterBase"/> class.
        /// </summary>
        /// <param name="textWriter">The text writer.</param>
        /// <param name="settings">The writer settings.</param>
        public OpenApiWriterBase(TextWriter textWriter, OpenApiWriterSettings settings)
        {
            Writer = textWriter;
            Writer.NewLine = "\n";

            this.scopes = new Stack<Scope>();
            this.settings = settings;
        }

        /// <summary>
        /// Write start object.
        /// </summary>
        public abstract void WriteStartObject();

        /// <summary>
        /// Write end object.
        /// </summary>
        public abstract void WriteEndObject();

        /// <summary>
        /// Write start array.
        /// </summary>
        public abstract void WriteStartArray();

        /// <summary>
        /// Write end array.
        /// </summary>
        public abstract void WriteEndArray();

        /// <summary>
        /// Write the start property.
        /// </summary>
        public virtual void WriteStartProperty(string name)
        {
            StartScope(ScopeType.Property);
        }

        /// <summary>
        /// Write the end property.
        /// </summary>
        public virtual void WriteEndProperty()
        {
            EndScope(ScopeType.Property);
        }

        /// <summary>
        /// Flush the writer.
        /// </summary>
        public void Flush()
        {
            this.Writer.Flush();
        }

        /// <summary>
        /// Write a double value.
        /// </summary>
        /// <param name="value">Double value to be written.</param>
        public virtual void WriteValue(double value)
        {
            this.WriteValueSeparator();
           // JsonValueUtils.WriteValue(this.writer, value);
        }

        public virtual void WriteValue(string value)
        {
        }

        public virtual void WriteValue(decimal value)
        {

        }

        public virtual void WriteValue(int value)
        {

        }

        public virtual void WriteValue(bool value)
        {

        }

        public abstract void WriteNull();

        public virtual void WriteValue(object value)
        {
            if (value == null)
            {
                WriteNull();
                return;
            }

            if (value is String)
            {
                WriteValue((String)(value));
            }
            else
            {
                // TODO:
            }
        }

        public virtual void WritePropertyName(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "The name must be specified.");
//            Debug.Assert(this.scopes.Count > 0, "There must be an active scope for name to be written.");
//            Debug.Assert(this.scopes.Peek().Type == ScopeType.Object, "The active scope must be an object scope for name to be written.");

            Scope currentScope = CurrentScope();
            Debug.Assert(currentScope != null);

            if (currentScope.ObjectCount != 0)
            {
                Writer.Write(JsonConstants.ObjectMemberSeparator);
            }
            Writer.WriteLine();

            currentScope.ObjectCount++;
        }

        /// <summary>
        /// Increases the level of indentation applied to the output.
        /// </summary>
        public virtual void IncreaseIndentation()
        {
            indentLevel++;
        }

        /// <summary>
        /// Decreases the level of indentation applied to the output.
        /// </summary>
        public virtual void DecreaseIndentation()
        {
            Debug.Assert(indentLevel > 0, "Trying to decrease indentation below zero.");
            if (indentLevel < 1)
            {
                indentLevel = 0;
            }
            else
            {
                indentLevel--;
            }
        }

        /// <summary>
        /// Write the indentation.
        /// </summary>
        public virtual void WriteIndentation()
        {
            for (int i = 0; i < (indentLevel + IndentShift); i++)
            {
                Writer.Write(IndentationString);
            }
        }

        public virtual void WritePrefixIndentation()
        {
            for (int i = 0; i < (indentLevel + IndentShift - 1); i++)
            {
                Writer.Write(IndentationString);
            }
        }

        /// <summary>
        /// Writes a separator of a value if it's needed for the next value to be written.
        /// </summary>
        protected void WriteValueSeparator()
        {
            if (scopes.Count == 0)
            {
                return;
            }

            Scope currentScope = this.scopes.Peek();
            if (currentScope.Type == ScopeType.Array)
            {
                if (currentScope.ObjectCount != 0)
                {
                    Writer.Write(JsonConstants.ArrayElementSeparator);
                }

                Writer.WriteLine();
                WriteIndentation();
                currentScope.ObjectCount++;
            }
        }

        /// <summary>
        /// Get current scope.
        /// </summary>
        /// <returns></returns>
        protected Scope CurrentScope()
        {
            return scopes.Count == 0 ? null : scopes.Peek();
        }

        /// <summary>
        /// Start the scope given the scope type.
        /// </summary>
        /// <param name="type">The scope type to start.</param>
        protected Scope StartScope(ScopeType type)
        {
            if (scopes.Count != 0)
            {
                Scope currentScope = this.scopes.Peek();
                if ((currentScope.Type == ScopeType.Array) &&
                    (currentScope.ObjectCount != 0))
                {
                    Writer.Write(JsonConstants.ArrayElementSeparator);
                }

                currentScope.ObjectCount++;
            }

            Scope scope = new Scope(type);
            this.scopes.Push(scope);
            return scope;
        }

        protected Scope EndScope(ScopeType type)
        {
            Debug.Assert(scopes.Count > 0, "No scope to end.");
            Debug.Assert(scopes.Peek().Type == type, "Ending scope does not match.");
            return scopes.Pop();
        }

        protected void IncreaseScopeObject()
        {
            ++this.scopes.Peek().ObjectCount;
        }

        protected bool IsObjectScope()
        {
            return IsScopeType(ScopeType.Object);
        }

        protected bool IsArrayScope()
        {
            return IsScopeType(ScopeType.Array);
        }

        protected bool IsPropertyScope()
        {
            return IsScopeType(ScopeType.Property);
        }

        private bool IsScopeType(ScopeType type)
        {
            if (scopes.Count == 0)
            {
                return false;
            }

            return scopes.Peek().Type == type;
        }

        protected void ValifyCanWritePropertyName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw Error.ArgumentNullOrEmpty(nameof(name));
            }

            if (this.scopes.Count == 0)
            {
                throw new OpenApiException(String.Format(SRResource.OpenApiWriterMustHaveActiveScope, name));
            }

            if (this.scopes.Peek().Type != ScopeType.Object)
            {
                throw new OpenApiException(String.Format(SRResource.OpenApiWriterMustBeObjectScope, name));
            }
        }
    }
}
