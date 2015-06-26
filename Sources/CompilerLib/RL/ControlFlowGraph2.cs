﻿using System.Collections.Generic;
using System.Linq;
using Dot42.CompilerLib.RL.Extensions;
using Dot42.DexLib.Instructions;
using Dot42.Utility;
using NinjaTools.Collections;

namespace Dot42.CompilerLib.RL
{
    internal class InstructionInBlock
    {
        public readonly Instruction Instruction;
        public readonly BasicBlock Block;

        public InstructionInBlock(Instruction instruction, BasicBlock block)
        {
            Instruction = instruction;
            Block = block;
        }

        public override string ToString()
        {
            return Instruction.Index.ToString("X2") + ": " + Instruction;
        }

        public bool IsDestinationRegister(int index)
        {
            var info = OpCodeInfo.Get(Instruction.Code.ToDex());
            var isDest = (info.GetUsage(index) & RegisterFlags.Destination) == RegisterFlags.Destination;

            // While not changing the value, check_cast changes the meaning of the
            // register. We treat it as a read-write instruction here. One should check if
            // check-cast should not always be treated as a read-write instruction.
            if (Instruction.Code == RCode.Check_cast)
                isDest = true;

            return isDest;
        }

        public bool IsSourceRegister(int index)
        {
            var info = OpCodeInfo.Get(Instruction.Code.ToDex());
            return (info.GetUsage(index) & RegisterFlags.Source) == RegisterFlags.Source;
        }
    }
    /// <summary>
    /// In addition to ControlFlowGraph, this class also takes exception handlers
    /// into account when calculating entry/exit blocks.
    /// </summary>
    public class ControlFlowGraph2
    {
        private readonly MethodBody _body;
        private readonly Dictionary<Instruction, BasicBlock> _entries;
        private readonly List<BasicBlock> _basicBlocks;
        private readonly List<Instruction> _entryInstructions;
        private Dictionary<BasicBlock, HashSet<BasicBlock>> _reachableBlocks;

        public List<BasicBlock> BasicBlocks { get { return _basicBlocks; } }

        public MethodBody Body { get { return _body; } }

        public ControlFlowGraph2(MethodBody body)
        {
            _body = body;
            _basicBlocks = new ControlFlowGraph(body).ToList();
            _entries = _basicBlocks.ToDictionary(b => b.Entry, b => b);
            _entryInstructions = _basicBlocks.Select(b => b.Entry).ToList();

            IncludeExceptionHandlers(body);
        }

        private void IncludeExceptionHandlers(MethodBody body)
        {
            foreach (var block in _basicBlocks)
                foreach (var ex in body.Exceptions)
                {
                    if (ex.TryStart.Index <= block.Entry.Index && ex.TryEnd.Index >= block.Exit.Index)
                    {
                        if (ex.CatchAll != null)
                        {
                            var exBlock = _entries[ex.CatchAll];
                            block.AddExitBlock(exBlock);
                            exBlock.AddEntryBlock(block);
                        }

                        foreach (var h in ex.Catches)
                        {
                            var exBlock = _entries[h.Instruction];
                            block.AddExitBlock(exBlock);
                            exBlock.AddEntryBlock(block);
                        }
                    }
                }
        }

        public HashSet<BasicBlock> GetReachableBlocks(BasicBlock basicBlock)
        {
            if (_reachableBlocks == null)
                CalculateReachableBlocks();
            return _reachableBlocks[basicBlock];
        }

        private void CalculateReachableBlocks()
        {
            _reachableBlocks = new Dictionary<BasicBlock, HashSet<BasicBlock>>();
            foreach (var block in TopologicalSort.Sort(_basicBlocks, p => p.ExitBlocks, ignoreCycles: true))
                _reachableBlocks[block] = CalculateReachableBlocks(block, new HashSet<BasicBlock>());
        }

        private HashSet<BasicBlock> CalculateReachableBlocks(BasicBlock block, HashSet<BasicBlock> visited)
        {
            visited.Add(block);

            HashSet<BasicBlock> ret = new HashSet<BasicBlock>();

            foreach (var b in block.ExitBlocks)
            {
                if (visited.Contains(b))
                    continue;

                ret.Add(b);
                if (_reachableBlocks.ContainsKey(b))
                {
                    foreach (var bb in _reachableBlocks[b])
                        ret.Add(bb);
                }
                else
                {
                    foreach (var dep in CalculateReachableBlocks(b, visited))
                        ret.Add(dep);
                }
            }

            return ret;
        }

        public BasicBlock BlockFromEntryInstruction(Instruction ins)
        {
            return _entries[ins];
        }

        public BasicBlock BlockFromInstruction(Instruction ins)
        {
            int idx = _entryInstructions.FindLastIndexSmallerThanOrEqualTo(ins.Index, a => a.Index);
            return _basicBlocks[idx];
        }

        /// <summary>
        /// This method tests if 'target' can be reached from 'source' without passing through 'blocker'
        /// </summary>
        internal bool IsReachable(InstructionInBlock target, InstructionInBlock source, InstructionInBlock blocker)
        {
            if (source.Block == target.Block && source.Block == blocker.Block)
                return target.Instruction.Index >= source.Instruction.Index 
                   && (source.Instruction.Index > blocker.Instruction.Index 
                       || blocker.Instruction.Index > target.Instruction.Index);
            if (source.Block == target.Block)
                return source.Instruction.Index <= target.Instruction.Index;
            if (source.Block == blocker.Block)
                return source.Instruction.Index > target.Instruction.Index;

            var queryReachables = GetReachableBlocks(source.Block);
            // the easy checks...
            if (!queryReachables.Contains(target.Block))
                return false;
            if (!queryReachables.Contains(blocker.Block))
                return true;
            // slightly more complicated: both last and first are reachable.
            if (target.Block == blocker.Block)
                return target.Instruction.Index < blocker.Instruction.Index;
            // work on a per-block basis.
            return IsReachable(target.Block, source.Block, blocker.Block, new HashSet<BasicBlock>());
        }

        /// <summary>
        /// This method tests if 'target' can be reached from 'source' without passing through 'blocker'
        /// </summary>
        private bool IsReachable(BasicBlock target, BasicBlock source, BasicBlock blocker, HashSet<BasicBlock> visited)
        {
            foreach (var exit in source.ExitBlocks)
            {                
                if (exit == target)
                    return true;
                if (exit == blocker || exit == source)
                    continue;
                if (visited.Contains(exit))
                    continue; 
                visited.Add(exit);
                if (IsReachable(target, exit, blocker, visited))
                    return true;
            }
            return false;
        }
    }
}
