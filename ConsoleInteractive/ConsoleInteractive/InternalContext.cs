﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConsoleInteractive {
    internal static class InternalContext {
        internal static object WriteLock = new();
        internal static Regex FormatRegex = new Regex("(§[0-9a-fk-orw-z])((?:[^§]|§[^0-9a-fk-orw-z])*)", RegexOptions.Compiled);
        internal static Regex ColorCodeRegex = new Regex(@"(\u001B\[([\d;]+)m)([^\u001B]*)", RegexOptions.Compiled);
        internal static volatile int CurrentCursorLeftPos = 0;
        internal static volatile int CurrentCursorTopPos = 0;
        internal static volatile int CursorLeftPosLimit = Console.BufferWidth;
        internal static volatile int CursorTopPosLimit = Console.BufferHeight;
        internal static volatile bool _suppressInput = false;
        internal static volatile bool BufferInitialized = false;
        internal static ColorMode ConsoleColorMode = ColorMode.None;

        internal enum ColorMode { 
            None,
            WindowsAPI,
            VTCode
        }

        internal static bool SuppressInput {
            get { return _suppressInput; }
            set {
                _suppressInput = value;
                if (value) {
                    ConsoleBuffer.ClearVisibleUserInput();
                }
                ConsoleBuffer.RedrawInput();
                ConsoleBuffer.MoveToEndBufferPosition();
            }
        }

        internal static void SetLeftCursorPosition(int leftPos) {
            if (CurrentCursorTopPos == CursorTopPosLimit) {
                Interlocked.Exchange(ref CurrentCursorTopPos, CursorTopPosLimit - 1);
            }
            
            Console.SetCursorPosition(leftPos, CurrentCursorTopPos);
            Interlocked.Exchange(ref CurrentCursorLeftPos, leftPos);
        }

        internal static void SetTopCursorPosition(int topPos) {
            if (CurrentCursorTopPos == CursorTopPosLimit) {
                Interlocked.Exchange(ref CurrentCursorTopPos, CursorTopPosLimit - 1);
            }
            
            Console.SetCursorPosition(CurrentCursorLeftPos, topPos);
            Interlocked.Exchange(ref CurrentCursorTopPos, topPos);
        }

        internal static void SetCursorPosition(int leftPos, int topPos) {
            SetLeftCursorPosition(leftPos);
            SetTopCursorPosition(topPos);
        }
        
        internal static void SetCursorVisible(bool visible) {
            
            // It's useful to have the cursor visible in debug situations
            #if DEBUG
                return;
            #endif
            
            Console.CursorVisible = visible;
        }

        internal static void IncrementLeftPos() {
            if (CursorLeftPosLimit <= CurrentCursorLeftPos + 1)
                return;
            CurrentCursorLeftPos = Interlocked.Increment(ref CurrentCursorLeftPos);
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
        
        internal static void DecrementLeftPos() {
            if (CurrentCursorLeftPos == 0)
                return;
            CurrentCursorLeftPos = Interlocked.Decrement(ref CurrentCursorLeftPos);
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
        
        internal static void IncrementTopPos() {
            if (CursorTopPosLimit <= CurrentCursorTopPos + 1)
                return;
            CurrentCursorTopPos = Interlocked.Increment(ref CurrentCursorTopPos);
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
    }
}