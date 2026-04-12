using FluentValidation;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Validators
{
    public class BookingValidator : AbstractValidator<Booking>
    {
        public BookingValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("Customer không được để trống");

            RuleFor(x => x.TripId)
                .NotEmpty().WithMessage("Trip không được để trống");

            RuleFor(x => x.SeatId)
                .NotEmpty().WithMessage("Seat không được để trống");
        }
    }
}