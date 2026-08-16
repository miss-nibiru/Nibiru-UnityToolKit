using System;
using UnityEngine;

namespace MissNibiru.Information.Data
{
    [Serializable]
    public sealed class InformationPage
    {
        [SerializeField]
        private string heading;

        [SerializeField, TextArea(3, 12)]
        private string body;

        [SerializeField]
        private Sprite image;

        public string Heading => heading;
        public string Body => body;
        public Sprite Image => image;

        public InformationPage()
        {
        }

        public InformationPage(
            string pageHeading,
            string pageBody,
            Sprite pageImage = null)
        {
            heading = pageHeading;
            body = pageBody;
            image = pageImage;
        }
    }
}