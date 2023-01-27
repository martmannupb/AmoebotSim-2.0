using System.Collections;
using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;

namespace AS2.Visuals
{
    public struct RenderBatchIndex
    {
        public int listNumber;
        public int listIndex;
        public bool isValid;

        public RenderBatchIndex(int listNumber, int listIndex)
        {
            this.listNumber = listNumber;
            this.listIndex = listIndex;
            this.isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        public void Discard()
        {
            isValid = false; ;
        }
    }

}