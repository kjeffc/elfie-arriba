using Arriba.Model.Correctors;
using System.Composition;

namespace Arriba.Communication.Model
{
    [Export(typeof(CompositionComposedCorrectors)), Shared]
    public class CompositionComposedCorrectors: ComposedCorrector
    {
        [ImportingConstructor]
        public CompositionComposedCorrectors() : base(new TodayCorrector())
        {

        }

        public CompositionComposedCorrectors(params ICorrector[] correctors) : base(correctors)
        {

        }
    }
}
